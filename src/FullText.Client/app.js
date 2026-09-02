(() => {
  "use strict";

  const API_BASE = window.API_BASE_URL || "/api";

  const entriesEl = document.getElementById("entries");
  const statusEl = document.getElementById("status");
  const paginationEl = document.getElementById("pagination");
  const pagePrevBtn = document.getElementById("page-prev");
  const pageNextBtn = document.getElementById("page-next");
  const pageLabelEl = document.getElementById("page-label");
  const formEl = document.getElementById("search-form");
  const inputEl = document.getElementById("search-input");
  const inputClearBtn = document.getElementById("input-clear-button");
  const clearBtn = document.getElementById("clear-button");

  const readerEl = document.getElementById("reader");
  const readerLoading = document.getElementById("reader-loading");
  const readerContent = document.getElementById("reader-content");
  const readerTitle = document.getElementById("reader-title");
  const readerDate = document.getElementById("reader-date");
  const readerBody = document.getElementById("reader-body");
  const readerClose = document.getElementById("reader-close");
  const downloadLink = document.getElementById("download-link");

  downloadLink.href = `${API_BASE}/blogs/download`;

  function formatDate(value) {
    if (!value) return "";
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return value;
    return d.toLocaleDateString(undefined, {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }

  function setStatus(text) {
    statusEl.textContent = text;
  }

  function clearEntries() {
    entriesEl.innerHTML = "";
  }

  function renderEmpty(message) {
    clearEntries();
    const li = document.createElement("li");
    li.className = "empty";
    li.textContent = message;
    entriesEl.appendChild(li);
  }

  function renderEntries(items, { withRelevance = false } = {}) {
    clearEntries();

    if (!items.length) {
      renderEmpty("No entries found.");
      return;
    }

    for (const item of items) {
      const li = document.createElement("li");
      const btn = document.createElement("button");
      btn.className = "entry";
      btn.type = "button";

      const top = document.createElement("div");
      top.className = "entry__top";

      const date = document.createElement("span");
      date.className = "entry__date";
      date.textContent = formatDate(item.duoDate ?? item.DuoDate);
      top.appendChild(date);

      if (withRelevance && item.relevance != null) {
        const rel = document.createElement("span");
        rel.className = "entry__relevance";
        // Server now sends raw ts_rank (Math.Round doesn't translate to SQL
        // via EF Core), so scale to a percentage and round to 2dp here.
        const relevancePercent = (item.relevance * 100).toFixed(2);
        rel.textContent = `${relevancePercent}% match`;
        top.appendChild(rel);
      }

      const title = document.createElement("h3");
      title.className = "entry__title";
      title.textContent = item.title ?? item.Title;

      const excerpt = document.createElement("p");
      excerpt.className = "entry__excerpt";
      excerpt.textContent = item.excerpt ?? item.Excerpt;

      btn.appendChild(top);
      btn.appendChild(title);
      btn.appendChild(excerpt);

      btn.addEventListener("click", () => openReader(item));

      li.appendChild(btn);
      entriesEl.appendChild(li);
    }
  }

  let openRequestId = 0;

  function showLoading() {
    readerLoading.hidden = false;
    readerContent.hidden = true;
  }

  function showContent({ title, date, body }) {
    readerTitle.textContent = title;
    readerDate.textContent = date;
    readerBody.textContent = body;
    readerLoading.hidden = true;
    readerContent.hidden = false;
  }

  async function openReader(item) {
    const titleText = item.title ?? item.Title;
    const dateText = formatDate(item.duoDate ?? item.DuoDate);
    const id = item.id ?? item.Id;

    const requestId = ++openRequestId;

    readerEl.hidden = false;
    document.body.style.overflow = "hidden";
    showLoading();
    readerClose.focus();

    // Content only ever comes from GET /blogs/{id} — the list and search
    // endpoints don't return Content at all. Search results don't carry
    // an id, so there's nothing to fetch for those; render immediately.
    if (id == null) {
      showContent({
        title: titleText,
        date: dateText,
        body: item.excerpt ?? item.Excerpt ?? "",
      });
      return;
    }

    try {
      const full = await fetchJson(`${API_BASE}/blogs/${id}`);
      if (requestId !== openRequestId) return; // a newer post was opened meanwhile
      showContent({
        title: titleText,
        date: dateText,
        body: full.content ?? full.Content ?? "",
      });
    } catch (err) {
      if (requestId !== openRequestId) return;
      showContent({
        title: titleText,
        date: dateText,
        body: "Couldn't load this post. Try again in a moment.",
      });
      console.error(err);
    }
  }

  function closeReader() {
    readerEl.hidden = true;
    document.body.style.overflow = "";
  }

  async function fetchJson(url) {
    const res = await fetch(url, { headers: { Accept: "application/json" } });
    if (!res.ok) {
      throw new Error(`Request failed: ${res.status} ${res.statusText}`);
    }
    return res.json();
  }

  const PAGE_SIZE = 5;
  let currentPage = 1;
  let currentMode = "browse"; // "browse" | "search"
  let currentSearchTerm = "";

  function hidePagination() {
    paginationEl.hidden = true;
  }

  function showPagination(page, totalPages) {
    currentPage = page;
    pageLabelEl.textContent = `Page ${page} of ${totalPages}`;
    pagePrevBtn.disabled = page <= 1;
    pageNextBtn.disabled = page >= totalPages;
    paginationEl.hidden = false;
  }

  async function loadBlogs(page = 1) {
    currentMode = "browse";
    setStatus("Loading archive…");
    try {
      const url = new URL(`${API_BASE}/blogs`, window.location.origin);
      url.searchParams.set("page", page);
      url.searchParams.set("pageSize", PAGE_SIZE);
      const result = await fetchJson(url.pathname + url.search);

      const items = result.items ?? result;
      renderEntries(items);

      if (result.totalPages != null) {
        showPagination(result.page ?? page, result.totalPages);
        setStatus(`${result.totalCount} ${result.totalCount === 1 ? "entry" : "entries"} in the archive.`);
      } else {
        // fallback if the API ever returns a bare array again
        hidePagination();
        setStatus(`${items.length} ${items.length === 1 ? "entry" : "entries"} in the archive.`);
      }
    } catch (err) {
      renderEmpty("Couldn't load the archive. Is the API running?");
      hidePagination();
      setStatus("");
      console.error(err);
    }
  }

  async function runSearch(term, page = 1) {
    currentMode = "search";
    currentSearchTerm = term;
    setStatus(`Searching for “${term}”…`);
    try {
      const url = new URL(`${API_BASE}/blogs/search`, window.location.origin);
      url.searchParams.set("searchTerms", term);
      url.searchParams.set("page", page);
      url.searchParams.set("pageSize", PAGE_SIZE);
      const result = await fetchJson(url.pathname + url.search);

      const items = result.items ?? result;
      renderEntries(items, { withRelevance: true });

      if (result.totalPages != null) {
        showPagination(result.page ?? page, result.totalPages);
        setStatus(
          `${result.totalCount} ${result.totalCount === 1 ? "result" : "results"} for “${term}”.`
        );
      } else {
        // fallback if the API ever returns a bare array again
        hidePagination();
        setStatus(`${items.length} ${items.length === 1 ? "result" : "results"} for “${term}”.`);
      }
    } catch (err) {
      renderEmpty("Search failed. Try again in a moment.");
      hidePagination();
      setStatus("");
      console.error(err);
    }
  }

  formEl.addEventListener("submit", (e) => {
    e.preventDefault();
    const term = inputEl.value.trim();
    if (!term) {
      loadBlogs(1);
      return;
    }
    runSearch(term);
  });

  clearBtn.addEventListener("click", () => {
    inputEl.value = "";
    toggleInputClear();
    loadBlogs(1);
  });

  function toggleInputClear() {
    inputClearBtn.hidden = inputEl.value.length === 0;
  }

  inputEl.addEventListener("input", toggleInputClear);

  inputClearBtn.addEventListener("click", () => {
    inputEl.value = "";
    toggleInputClear();
    inputEl.focus();
    loadBlogs(1);
  });

  pagePrevBtn.addEventListener("click", () => {
    if (currentPage <= 1) return;
    if (currentMode === "search") {
      runSearch(currentSearchTerm, currentPage - 1);
    } else {
      loadBlogs(currentPage - 1);
    }
  });

  pageNextBtn.addEventListener("click", () => {
    if (currentMode === "search") {
      runSearch(currentSearchTerm, currentPage + 1);
    } else {
      loadBlogs(currentPage + 1);
    }
  });

  readerClose.addEventListener("click", closeReader);
  readerEl.addEventListener("click", (e) => {
    if (e.target === readerEl) closeReader();
  });
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !readerEl.hidden) closeReader();
  });

  loadBlogs(1);
})();
