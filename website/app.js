// Interactive state and live GitHub release downloader
(function () {
  const GITHUB_REPO = "Kartik-181123/To-Do-App";

  // Initial sample tasks mirroring real workbench state
  let tasks = [
    {
      id: 1,
      title: "Ship v6.0.0 Studio Workbench release",
      desc: "Finalize Fluent 2 dark workbench, 3-pane layout, and process termination safeguards.",
      priority: "High",
      done: true,
      time: "Today"
    },
    {
      id: 2,
      title: "Clean desktop widget wallpaper docking",
      desc: "Ensure seamless click-through, opacity slider, and corner pinning behind open windows.",
      priority: "Medium",
      done: false,
      time: "2:30 PM"
    },
    {
      id: 3,
      title: "Sync streaks with GitHub Gist",
      desc: "Verify two-way sync across multiple Windows devices with zero conflict.",
      priority: "Medium",
      done: false,
      time: "5:00 PM"
    },
    {
      id: 4,
      title: "Test Focus Mode distraction blocker",
      desc: "Validate automatic process termination for blocked apps and Task Manager guard.",
      priority: "Low",
      done: false,
      time: "Tomorrow"
    }
  ];

  let selectedTaskId = 2;
  let activeFilter = "all";

  // DOM Elements
  const wbTaskList = document.getElementById("wbTaskList");
  const dwTaskList = document.getElementById("dwTaskList");
  const countAll = document.getElementById("countAll");
  const countToday = document.getElementById("countToday");
  const countHigh = document.getElementById("countHigh");
  const countDone = document.getElementById("countDone");
  const canvasTitle = document.getElementById("canvasTitle");
  const inspTitle = document.getElementById("inspTitle");
  const inspMeta = document.getElementById("inspMeta");
  const inspDesc = document.getElementById("inspDesc");
  const inspToggleBtn = document.getElementById("inspToggleBtn");
  const dockFraction = document.getElementById("dockFraction");
  const dockProgressBar = document.getElementById("dockProgressBar");
  const dwFraction = document.getElementById("dwFraction");
  const dwProgressBar = document.getElementById("dwProgressBar");
  const quickAddForm = document.getElementById("quickAddForm");
  const quickAddInput = document.getElementById("quickAddInput");

  // Timer state
  let timerDuration = 120;
  let timerRemaining = 120;
  let timerInterval = null;
  const timerReadout = document.getElementById("timerReadout");
  const timerPlayBtn = document.getElementById("timerPlayBtn");
  const timerResetBtn = document.getElementById("timerResetBtn");
  const timerModeButtons = document.querySelectorAll(".timer-mode");

  // Theme toggle
  const themeToggle = document.getElementById("themeToggle");
  const currentTheme = localStorage.getItem("todo_theme") || "dark";
  document.documentElement.setAttribute("data-theme", currentTheme);

  themeToggle?.addEventListener("click", () => {
    const active = document.documentElement.getAttribute("data-theme") === "light" ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", active);
    localStorage.setItem("todo_theme", active);
  });

  // Tab switching: Workbench vs Widget
  const segButtons = document.querySelectorAll(".seg-btn");
  const viewWorkbench = document.getElementById("view-workbench");
  const viewWidget = document.getElementById("view-widget");

  segButtons.forEach((btn) => {
    btn.addEventListener("click", () => {
      segButtons.forEach((b) => {
        b.classList.remove("active");
        b.setAttribute("aria-selected", "false");
      });
      btn.classList.add("active");
      btn.setAttribute("aria-selected", "true");

      const view = btn.dataset.view;
      if (view === "widget") {
        viewWorkbench.classList.add("hidden");
        viewWidget.classList.remove("hidden");
      } else {
        viewWidget.classList.add("hidden");
        viewWorkbench.classList.remove("hidden");
      }
    });
  });

  // Filter switching
  const navItems = document.querySelectorAll(".wb-nav-item");
  navItems.forEach((btn) => {
    btn.addEventListener("click", () => {
      navItems.forEach((b) => b.classList.remove("active"));
      btn.classList.add("active");
      activeFilter = btn.dataset.filter || "all";
      const filterLabels = {
        all: "All Tasks",
        today: "Today's Schedule",
        high: "High Priority",
        completed: "Completed Tasks"
      };
      if (canvasTitle) canvasTitle.textContent = filterLabels[activeFilter] || "Tasks";
      renderTasks();
    });
  });

  function getFilteredTasks() {
    return tasks.filter((t) => {
      if (activeFilter === "today") return t.time.toLowerCase().includes("today") || t.time.includes("PM");
      if (activeFilter === "high") return t.priority === "High";
      if (activeFilter === "completed") return t.done;
      return true;
    });
  }

  function renderTasks() {
    const filtered = getFilteredTasks();
    const completedCount = tasks.filter((t) => t.done).length;
    const totalCount = tasks.length;
    const percent = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

    // Badge counts
    if (countAll) countAll.textContent = totalCount;
    if (countToday) countToday.textContent = tasks.filter((t) => t.time.toLowerCase().includes("today") || t.time.includes("PM")).length;
    if (countHigh) countHigh.textContent = tasks.filter((t) => t.priority === "High").length;
    if (countDone) countDone.textContent = completedCount;

    // Progress updates
    const fractionText = `${completedCount} of ${totalCount} completed`;
    if (dockFraction) dockFraction.textContent = fractionText;
    if (dockProgressBar) dockProgressBar.style.width = `${percent}%`;
    if (dwFraction) dwFraction.textContent = `${completedCount}/${totalCount}`;
    if (dwProgressBar) dwProgressBar.style.width = `${percent}%`;

    // Render Workbench list
    if (wbTaskList) {
      wbTaskList.innerHTML = "";
      if (filtered.length === 0) {
        const empty = document.createElement("div");
        empty.style.padding = "24px";
        empty.style.textAlign = "center";
        empty.style.color = "#67728e";
        empty.style.fontSize = "13px";
        empty.textContent = "No tasks in this collection.";
        wbTaskList.appendChild(empty);
      } else {
        filtered.forEach((task) => {
          const item = document.createElement("div");
          item.className = `wb-task ${task.done ? "done" : ""} ${task.id === selectedTaskId ? "selected" : ""}`;
          item.innerHTML = `
            <button class="wb-check" type="button" aria-label="Toggle complete">
              ${task.done ? `<svg width="10" height="10" viewBox="0 0 12 12" fill="none"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>` : ""}
            </button>
            <span class="wb-task-title">${escapeHtml(task.title)}</span>
            <span class="prio-tag prio-${task.priority}">${task.priority}</span>
          `;

          item.addEventListener("click", (e) => {
            if (e.target.closest(".wb-check")) {
              toggleTask(task.id);
            } else {
              selectedTaskId = task.id;
              renderTasks();
            }
          });
          wbTaskList.appendChild(item);
        });
      }
    }

    // Render Widget preview list
    if (dwTaskList) {
      dwTaskList.innerHTML = "";
      tasks.slice(0, 3).forEach((task) => {
        const row = document.createElement("div");
        row.className = `wb-task ${task.done ? "done" : ""}`;
        row.style.padding = "6px 10px";
        row.style.background = "rgba(255,255,255,0.03)";
        row.style.border = "1px solid rgba(255,255,255,0.06)";
        row.innerHTML = `
          <button class="wb-check" type="button" aria-label="Toggle complete">
            ${task.done ? `<svg width="10" height="10" viewBox="0 0 12 12" fill="none"><path d="M2.5 6L5 8.5L9.5 3.5" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>` : ""}
          </button>
          <span class="wb-task-title" style="font-size: 12px">${escapeHtml(task.title)}</span>
        `;
        row.querySelector(".wb-check").addEventListener("click", () => toggleTask(task.id));
        dwTaskList.appendChild(row);
      });
    }

    // Update Details Inspector
    const current = tasks.find((t) => t.id === selectedTaskId) || tasks[0];
    if (current && inspTitle) {
      inspTitle.textContent = current.title;
      inspMeta.textContent = `${current.priority} Priority \u00B7 ${current.time}`;
      inspDesc.textContent = current.desc;
      inspToggleBtn.textContent = current.done ? "Mark as Pending" : "Mark Complete";
    }
  }

  function toggleTask(id) {
    const task = tasks.find((t) => t.id === id);
    if (!task) return;
    task.done = !task.done;
    renderTasks();
  }

  inspToggleBtn?.addEventListener("click", () => {
    if (selectedTaskId) toggleTask(selectedTaskId);
  });

  // Quick-add from dock
  quickAddForm?.addEventListener("submit", (e) => {
    e.preventDefault();
    const title = quickAddInput.value.trim();
    if (!title) return;

    const newTask = {
      id: Date.now(),
      title: title,
      desc: "Added via quick entry dock.",
      priority: "Medium",
      done: false,
      time: "Today"
    };
    tasks.unshift(newTask);
    selectedTaskId = newTask.id;
    quickAddInput.value = "";
    renderTasks();
  });

  // Dock sprint timer
  function formatTime(sec) {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  }

  timerModeButtons.forEach((btn) => {
    btn.addEventListener("click", () => {
      timerModeButtons.forEach((b) => b.classList.remove("active"));
      btn.classList.add("active");
      clearInterval(timerInterval);
      timerInterval = null;
      timerPlayBtn.textContent = "Start";
      timerDuration = parseInt(btn.dataset.seconds, 10) || 120;
      timerRemaining = timerDuration;
      timerReadout.textContent = formatTime(timerRemaining);
    });
  });

  timerPlayBtn?.addEventListener("click", () => {
    if (timerInterval) {
      clearInterval(timerInterval);
      timerInterval = null;
      timerPlayBtn.textContent = "Start";
    } else {
      timerPlayBtn.textContent = "Pause";
      timerInterval = setInterval(() => {
        timerRemaining--;
        if (timerRemaining <= 0) {
          clearInterval(timerInterval);
          timerInterval = null;
          timerPlayBtn.textContent = "Start";
          timerRemaining = timerDuration;
        }
        timerReadout.textContent = formatTime(timerRemaining);
      }, 1000);
    }
  });

  timerResetBtn?.addEventListener("click", () => {
    clearInterval(timerInterval);
    timerInterval = null;
    timerPlayBtn.textContent = "Start";
    timerRemaining = timerDuration;
    timerReadout.textContent = formatTime(timerRemaining);
  });

  // Live GitHub Releases API Integration
  async function fetchLatestRelease() {
    const downloadBtn = document.getElementById("downloadBtn");
    const downloadBtnText = document.getElementById("downloadBtnText");
    const releaseBadge = document.getElementById("releaseBadge");
    const releaseMeta = document.getElementById("releaseMeta");

    try {
      const res = await fetch(`https://api.github.com/repos/${GITHUB_REPO}/releases/latest`);
      if (!res.ok) throw new Error("Release fetch failed");
      const data = await res.json();

      const version = data.tag_name || "v6.1.0";
      // Find installer asset (ToDoSetup-*.exe)
      const asset = (data.assets || []).find((a) => a.name.endsWith(".exe"));

      if (releaseBadge) {
        releaseBadge.textContent = `${version} \u00B7 Stability & Polish`;
      }

      if (asset) {
        const sizeMB = (asset.size / (1024 * 1024)).toFixed(1);
        if (downloadBtn) downloadBtn.href = asset.browser_download_url;
        if (downloadBtnText) downloadBtnText.textContent = `Download ${asset.name}`;
        if (releaseMeta) {
          releaseMeta.textContent = `Windows 10 & 11 (64-bit) \u00B7 ${sizeMB} MB \u00B7 Free & Open Source`;
        }
      }
    } catch (_) {
      // Graceful fallback to default v6.0.0 link if offline or rate-limited
    }
  }

  function escapeHtml(str) {
    return str
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  renderTasks();
  fetchLatestRelease();
})();
