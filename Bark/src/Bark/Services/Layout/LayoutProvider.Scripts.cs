using Bark.Services.Rendering;

namespace Bark.Services.Layout;

public static partial class LayoutProvider
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, GeneratedAsset> _jsAssets = new(StringComparer.OrdinalIgnoreCase);

    private static string GetScriptsTag(bool enableLiveReload, bool enableDarkMode, long buildVersion, string basePath, bool staticSearch, Localization localization, bool isRootLocale)
    {
        var asset = GetScriptsAsset(enableLiveReload, enableDarkMode, buildVersion, basePath, staticSearch, localization, isRootLocale);
        return $"<script defer src=\"{basePath}/bark.js?v={asset.Version}&amp;locale={Uri.EscapeDataString(localization.Code)}\"></script>";
    }

    internal static GeneratedAsset GetScriptsAsset(bool enableLiveReload, bool enableDarkMode, long buildVersion, string basePath, bool staticSearch, Localization localization, bool isRootLocale) =>
        GetOrBuildAsset(_jsAssets, localization.Code, $"{enableLiveReload} {enableDarkMode} {buildVersion} {basePath} {staticSearch} {localization.Code} {isRootLocale}",
            () => BuildScriptsBody(enableLiveReload, enableDarkMode, buildVersion, basePath, staticSearch, localization, isRootLocale));

    private static string BuildScriptsBody(bool enableLiveReload, bool enableDarkMode, long buildVersion, string basePath, bool staticSearch, Localization localization, bool isRootLocale)
    {
        var l = localization;
        // Non-toggle theme swaps (bfcache/other tab/OS flip) hit an already-painted page: suppress transitions or every element cross-fades.
        var themeSyncScript = enableDarkMode
            ? @"var themeRoot = document.documentElement;
        var prefersDark = window.matchMedia('(prefers-color-scheme: dark)');
        function syncThemeToggle() {
            var toggle = document.getElementById('theme-toggle');
            if (!toggle) return;
            var current = themeRoot.getAttribute('data-theme');
            toggle.setAttribute('aria-checked', String(current ? current === 'dark' : prefersDark.matches));
        }
        function applyStoredTheme() {
            var t = null;
            try { t = localStorage.getItem('bark-theme'); } catch (_) {}
            themeRoot.classList.add('no-theme-transition');
            if (t === 'dark' || t === 'light') {
                themeRoot.setAttribute('data-theme', t);
                themeRoot.style.colorScheme = t;
            } else {
                themeRoot.removeAttribute('data-theme');
                themeRoot.style.colorScheme = '';
            }
            requestAnimationFrame(function() {
                requestAnimationFrame(function() {
                    themeRoot.classList.remove('no-theme-transition');
                });
            });
            syncThemeToggle();
        }
        window.addEventListener('pageshow', function(e) {
            if (e.persisted) applyStoredTheme();
        });
        window.addEventListener('storage', function(e) {
            if (e.key === 'bark-theme') applyStoredTheme();
        });
        prefersDark.addEventListener('change', syncThemeToggle);"
            : "";

        return MinifyJs($@"
        {themeSyncScript}
        document.addEventListener('DOMContentLoaded', function() {{
            var headings = document.querySelectorAll('.content h1, .content h2, .content h3, .content h4');
            var tocItems = document.querySelectorAll('.toc-item');
            var tocIndicator = document.querySelector('.toc-indicator');
            var tocListWrapper = document.querySelector('.toc-list-wrapper');
            var scrollIndicator = document.getElementById('scroll-indicator');
            var menuToggle = document.getElementById('menu-toggle');
            var sidebarLeft = document.getElementById('sidebar-left');
            var sidebarOverlay = document.getElementById('sidebar-overlay');
            var themeToggle = document.getElementById('theme-toggle');
            var topbarEl = document.querySelector('.topbar');
            var mainContainerEl = document.querySelector('.main-container');
            var sidebarRightEl = document.getElementById('sidebar-right');
            var sidebarLastFocused = null;

            function setInert(nodes, active) {{
                nodes.forEach(function(n) {{ if (n) n.inert = active; }});
            }}

            function getFocusable(container) {{
                return Array.prototype.slice.call(
                    container.querySelectorAll('a[href], button:not([disabled]), input, [tabindex]:not([tabindex=""-1""])'));
            }}

            if ({(enableLiveReload ? "true" : "false")}) {{
                var currentBuildVersion = {buildVersion};
                setInterval(function() {{
                    fetch('{basePath}/api/build-version')
                        .then(function(r) {{ return r.json(); }})
                        .then(function(data) {{
                            if (data.version !== currentBuildVersion) {{
                                location.reload();
                            }}
                        }})
                        ['catch'](function() {{}});
                }}, 5000);
            }}

            if (themeToggle) {{
                syncThemeToggle();

                themeToggle.addEventListener('click', function() {{
                    var current = themeRoot.getAttribute('data-theme');
                    var isDark = current ? current === 'dark' : prefersDark.matches;
                    var next = isDark ? 'light' : 'dark';
                    themeRoot.setAttribute('data-theme', next);
                    themeRoot.style.colorScheme = next;
                    themeToggle.setAttribute('aria-checked', String(next === 'dark'));
                    try {{ localStorage.setItem('bark-theme', next); }} catch (e) {{}}

                    // Mermaid bakes theme colors into its SVG at render time; reload to re-render.
                    if (document.querySelector('.mermaid')) {{
                        location.reload();
                    }}
                }});
            }}

            if (sidebarLeft) {{
                function closeSidebar() {{
                    sidebarLeft.classList.remove('open');
                    sidebarOverlay.classList.remove('open');
                    menuToggle.setAttribute('aria-expanded', 'false');
                    document.documentElement.style.overflow = '';
                    setInert([topbarEl, mainContainerEl, sidebarRightEl], false);
                    if (sidebarLastFocused && typeof sidebarLastFocused.focus === 'function') {{
                        sidebarLastFocused.focus();
                    }}
                }}

                function setDrawerTop() {{
                    if (topbarEl) document.documentElement.style.setProperty('--drawer-top', Math.max(0, topbarEl.getBoundingClientRect().bottom) + 'px');
                }}
                window.addEventListener('resize', function() {{
                    if (sidebarLeft.classList.contains('open')) setDrawerTop();
                }});

                function openSidebar() {{
                    sidebarLastFocused = document.activeElement;
                    setDrawerTop();
                    sidebarLeft.classList.add('open');
                    sidebarOverlay.classList.add('open');
                    menuToggle.setAttribute('aria-expanded', 'true');
                    // Drawer is position:fixed; without this, touch-scroll on it scrolls <body> underneath.
                    document.documentElement.style.overflow = 'hidden';
                    setInert([topbarEl, mainContainerEl, sidebarRightEl], true);
                    var focusable = getFocusable(sidebarLeft);
                    (focusable[0] || sidebarLeft).focus();
                }}

                menuToggle.addEventListener('click', function() {{
                    if (sidebarLeft.classList.contains('open')) {{ closeSidebar(); }} else {{ openSidebar(); }}
                }});
                sidebarOverlay.addEventListener('click', closeSidebar);
                document.addEventListener('keydown', function(e) {{
                    if (!sidebarLeft.classList.contains('open')) return;
                    if (e.key === 'Escape') {{ closeSidebar(); return; }}
                    if (e.key === 'Tab') {{
                        var focusable = getFocusable(sidebarLeft);
                        if (!focusable.length) return;
                        var first = focusable[0], last = focusable[focusable.length - 1];
                        if (e.shiftKey && document.activeElement === first) {{ e.preventDefault(); last.focus(); }}
                        else if (!e.shiftKey && document.activeElement === last) {{ e.preventDefault(); first.focus(); }}
                    }}
                }});
                sidebarLeft.querySelectorAll('.nav-item a').forEach(function(link) {{
                    link.addEventListener('click', closeSidebar);
                }});

                var touchStartX = null;
                sidebarLeft.addEventListener('touchstart', function(e) {{
                    touchStartX = e.touches[0].clientX;
                }}, {{ passive: true }});
                sidebarLeft.addEventListener('touchmove', function(e) {{
                    if (touchStartX === null) return;
                    var deltaX = e.touches[0].clientX - touchStartX;
                    if (deltaX < -40) {{
                        closeSidebar();
                        touchStartX = null;
                    }}
                }}, {{ passive: true }});
                sidebarLeft.addEventListener('touchend', function() {{ touchStartX = null; }});

                var activeNavItem = sidebarLeft.querySelector('.nav-item.active, .sidebar-link.is-active');
                if (activeNavItem) {{
                    var itemRect = activeNavItem.getBoundingClientRect();
                    var sidebarRect = sidebarLeft.getBoundingClientRect();
                    if (itemRect.top < sidebarRect.top || itemRect.bottom > sidebarRect.bottom) {{
                        sidebarLeft.scrollTop += (itemRect.top - sidebarRect.top) - (sidebarLeft.clientHeight - itemRect.height) / 2;
                    }}
                }}
            }}

            function updateScrollProgress() {{
                if (!scrollIndicator) return;
                var winScroll = document.documentElement.scrollTop || document.body.scrollTop;
                var height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
                var scrolled = height > 0 ? (winScroll / height) * 100 : 0;
                scrollIndicator.style.transform = 'scaleX(' + (scrolled / 100) + ')';
            }}

            window.addEventListener('scroll', updateScrollProgress);

            function updateTocIndicator() {{
                if (!tocIndicator || !tocListWrapper) return;
                var activeLink = tocListWrapper.querySelector('.toc-item.active > a');
                if (!activeLink) {{
                    tocIndicator.classList.remove('visible');
                    return;
                }}
                var wrapperTop = tocListWrapper.getBoundingClientRect().top;
                var linkRect = activeLink.getBoundingClientRect();
                tocIndicator.style.transform = 'translateY(' + (linkRect.top - wrapperTop) + 'px) scaleY(' + linkRect.height + ')';
                tocIndicator.classList.add('visible');
            }}

            // toc-item markup renders twice (tablet dropdown + desktop sidebar), so activation is keyed by heading id.
            function setActiveTocId(id) {{
                tocItems.forEach(function(item) {{
                    var link = item.querySelector('a');
                    var matches = link && link.getAttribute('href') === '#' + id;
                    item.classList.toggle('active', !!matches);
                }});
                updateTocIndicator();
            }}

            var observer = new IntersectionObserver(function(entries) {{
                entries.forEach(function(entry) {{
                    if (entry.isIntersecting) {{
                        setActiveTocId(entry.target.getAttribute('id'));
                    }}
                }});
            }}, {{ root: null, rootMargin: '-5% 0px -75% 0px', threshold: 0 }});

            headings.forEach(function(h) {{ observer.observe(h); }});

            // Native #hash scroll happens before JS; a heading flush at the top sits outside the observer rootMargin and never fires.
            if (location.hash) {{
                setActiveTocId(location.hash.slice(1));
            }}
            window.addEventListener('hashchange', function() {{
                if (location.hash) setActiveTocId(location.hash.slice(1));
            }});

            // At page bottom the last heading can sit above the observer rootMargin, leaving the wrong item active.
            function checkScrolledToBottom() {{
                var atBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2;
                if (atBottom && headings.length > 0) {{
                    setActiveTocId(headings[headings.length - 1].getAttribute('id'));
                }}
            }}

            window.addEventListener('scroll', checkScrolledToBottom, {{ passive: true }});
            window.addEventListener('resize', updateTocIndicator);
            checkScrolledToBottom();

            var pageLayout = document.querySelector('.layout');
            var tocCollapseToggle = document.getElementById('toc-collapse-toggle');
            if (pageLayout && sidebarRightEl && tocCollapseToggle) {{
                function applyTocCollapsed(collapsed) {{
                    pageLayout.classList.toggle('toc-collapsed', collapsed);
                    sidebarRightEl.hidden = collapsed;
                    tocCollapseToggle.setAttribute('aria-expanded', String(!collapsed));
                    var label = tocCollapseToggle.getAttribute(collapsed ? 'data-label-show' : 'data-label-hide');
                    tocCollapseToggle.setAttribute('aria-label', label);
                    tocCollapseToggle.setAttribute('title', label);
                    if (!collapsed) updateTocIndicator();
                }}

                var storedToc = null;
                try {{ storedToc = localStorage.getItem('bark-toc-collapsed'); }} catch (_) {{}}
                if (storedToc === '1') applyTocCollapsed(true);

                tocCollapseToggle.addEventListener('click', function() {{
                    var collapsed = !pageLayout.classList.contains('toc-collapsed');
                    applyTocCollapsed(collapsed);
                    try {{ localStorage.setItem('bark-toc-collapsed', collapsed ? '1' : '0'); }} catch (_) {{}}
                }});
            }}

            document.querySelectorAll('.top-nav-item.has-dropdown').forEach(function(item) {{
                var trigger = item.querySelector('.top-nav-link');
                var menu = item.querySelector('.top-nav-dropdown-menu');
                if (!trigger || !menu) return;
                item.addEventListener('focusin', function() {{ trigger.setAttribute('aria-expanded', 'true'); }});
                item.addEventListener('focusout', function(e) {{
                    if (!item.contains(e.relatedTarget)) trigger.setAttribute('aria-expanded', 'false');
                }});
                item.addEventListener('keydown', function(e) {{
                    if (e.key === 'Escape') {{
                        trigger.setAttribute('aria-expanded', 'false');
                        trigger.focus();
                    }}
                }});
            }});

            tocItems.forEach(function(item) {{
                var link = item.querySelector('a');
                if (link) {{
                    link.addEventListener('click', function() {{
                        setActiveTocId(link.getAttribute('href').slice(1));
                    }});
                }}
            }});

            var searchTrigger = document.getElementById('search-trigger');
            var searchTriggerKbd = document.getElementById('search-trigger-kbd');
            var searchOverlay = document.getElementById('search-overlay');
            var searchModalInput = document.getElementById('search-modal-input');
            var searchModalResults = document.getElementById('search-modal-results');
            var searchModalClose = document.getElementById('search-modal-close');
            var searchModalStatus = document.getElementById('search-modal-status');
            var searchTimeout;
            var searchActiveIndex = -1;
            var searchLastFocused = null;
            var searchRequestId = 0;

            // Static export: search runs in-browser against a prebuilt index, mirroring SearchIndex.
            var barkStaticSearch = {(staticSearch ? "true" : "false")};
            var barkLocale = '{Localization.JsEncode(localization.Code)}';
            var barkSearchIndexUrl = '{basePath}{(isRootLocale ? "" : "/" + localization.Code)}/search-index.json';
            var barkSearchIndexPromise = null;
            var BARK_MAX_QUERY_LENGTH = 128, BARK_MAX_QUERY_TERMS = 8, BARK_MAX_FUZZY = 3, BARK_FUZZY_THRESHOLD = 0.5;

            function barkLoadSearchIndex() {{
                if (!barkSearchIndexPromise) {{
                    barkSearchIndexPromise = fetch(barkSearchIndexUrl).then(function(r) {{
                        if (!r.ok) throw new Error('search index unavailable');
                        return r.json();
                    }});
                }}
                return barkSearchIndexPromise;
            }}

            function barkTokenize(text) {{
                if (!text) return [];
                return text.split(/\W+/)
                    .map(function(w) {{ return w.trim().toLowerCase(); }})
                    .filter(function(w) {{ return w.length > 0; }});
            }}

            function barkTrigrams(term) {{
                if (term.length < 3) return [];
                var out = [];
                for (var i = 0; i <= term.length - 3; i++) out.push(term.substr(i, 3));
                return out;
            }}

            function barkExcerpt(text, term) {{
                if (!text) return null;
                var idx = text.toLowerCase().indexOf(term.toLowerCase());
                if (idx < 0) return null;
                var start = Math.max(0, idx - 60);
                var length = Math.min(text.length - start, 160);
                var excerpt = text.slice(start, start + length).trim();
                if (start > 0) excerpt = '...' + excerpt;
                if (start + length < text.length) excerpt = excerpt + '...';
                return excerpt;
            }}

            function barkFindFuzzy(index, term) {{
                var tg = barkTrigrams(term);
                if (!tg.length) return [];
                var shared = {{}};
                tg.forEach(function(t) {{
                    var terms = index.trigrams[t];
                    if (!terms) return;
                    terms.forEach(function(it) {{ shared[it] = (shared[it] || 0) + 1; }});
                }});
                var cands = [];
                Object.keys(shared).forEach(function(it) {{
                    var countB = Math.max(it.length - 2, 0);
                    var denom = tg.length + countB;
                    var sim = denom === 0 ? 0 : (2 * shared[it]) / denom;
                    if (sim >= BARK_FUZZY_THRESHOLD) cands.push({{ term: it, sim: sim }});
                }});
                cands.sort(function(a, b) {{ return b.sim - a.sim || (a.term < b.term ? -1 : a.term > b.term ? 1 : 0); }});
                return cands.slice(0, BARK_MAX_FUZZY).map(function(c) {{ return c.term; }});
            }}

            function barkAccumulate(index, scores, postings, excerptTerm, divisor) {{
                postings.forEach(function(p) {{
                    var doc = index.docs[p.doc];
                    if (!doc) return;
                    var eff = Math.max(1, Math.floor(p.score / divisor));
                    var ex = barkExcerpt(doc.text, excerptTerm);
                    if (ex == null && doc.description) ex = barkExcerpt(doc.description, excerptTerm);
                    var cur = scores[p.doc];
                    if (cur) {{ cur.score += eff; if (cur.excerpt == null) cur.excerpt = ex; }}
                    else scores[p.doc] = {{ score: eff, excerpt: ex }};
                }});
            }}

            function barkSearchStatic(index, query) {{
                if (query.length > BARK_MAX_QUERY_LENGTH) query = query.slice(0, BARK_MAX_QUERY_LENGTH);
                var terms = barkTokenize(query);
                if (!terms.length) return [];
                if (terms.length > BARK_MAX_QUERY_TERMS) terms = terms.slice(0, BARK_MAX_QUERY_TERMS);
                var scores = {{}};
                terms.forEach(function(term) {{
                    var key = term.toLowerCase();
                    if (index.terms[key]) {{ barkAccumulate(index, scores, index.terms[key], term, 1); return; }}
                    barkFindFuzzy(index, key).forEach(function(cand) {{
                        if (index.terms[cand]) barkAccumulate(index, scores, index.terms[cand], cand, 2);
                    }});
                }});
                return Object.keys(scores).map(function(k) {{
                    var doc = index.docs[k];
                    return {{ path: doc.path, title: doc.title, description: doc.description, excerpt: scores[k].excerpt, score: scores[k].score }};
                }}).sort(function(a, b) {{ return b.score - a.score || (a.path < b.path ? -1 : a.path > b.path ? 1 : 0); }});
            }}

            function barkRunSearch(query) {{
                if (barkStaticSearch) {{
                    return barkLoadSearchIndex().then(function(index) {{ return barkSearchStatic(index, query); }});
                }}
                return fetch('{basePath}/api/search?q=' + encodeURIComponent(query) + '&locale=' + encodeURIComponent(barkLocale)).then(function(r) {{ return r.json(); }});
            }}

            if (searchTriggerKbd && /Mac|iPhone|iPad/.test(navigator.platform || '')) {{
                searchTriggerKbd.textContent = '⌘K';
            }}

            function escapeHtml(value) {{
                var div = document.createElement('div');
                div.textContent = value == null ? '' : value;
                return div.innerHTML;
            }}

            function escapeRegExp(value) {{
                return value.replace(/[.*+?^${{}}()|[\]\\]/g, '\\$&');
            }}

            // Escape first, then highlight within the escaped string so the <mark> wrap can't reopen an XSS hole.
            function highlightMatches(value, terms) {{
                var escaped = escapeHtml(value);
                if (!terms.length) return escaped;
                var pattern = new RegExp('(' + terms.map(escapeRegExp).join('|') + ')', 'ig');
                return escaped.replace(pattern, '<mark class=""search-highlight"">$1</mark>');
            }}

            function getSearchResultItems() {{
                return Array.prototype.slice.call(searchModalResults.querySelectorAll('.search-result-item'));
            }}

            function setSearchActiveIndex(index) {{
                var items = getSearchResultItems();
                if (items.length === 0) {{
                    searchActiveIndex = -1;
                    searchModalInput.removeAttribute('aria-activedescendant');
                    return;
                }}
                searchActiveIndex = (index + items.length) % items.length;
                items.forEach(function(item, i) {{
                    var isActive = i === searchActiveIndex;
                    item.classList.toggle('active', isActive);
                    item.setAttribute('aria-selected', isActive ? 'true' : 'false');
                }});
                var activeItem = items[searchActiveIndex];
                searchModalInput.setAttribute('aria-activedescendant', activeItem.id);
                activeItem.scrollIntoView({{ block: 'nearest' }});
            }}

            function openSearchModal() {{
                searchLastFocused = document.activeElement;
                searchOverlay.hidden = false;
                requestAnimationFrame(function() {{ searchOverlay.classList.add('open'); }});
                document.documentElement.style.overflow = 'hidden';
                searchModalInput.value = '';
                searchModalInput.focus();
                searchModalResults.innerHTML = '';
                searchModalInput.setAttribute('aria-expanded', 'false');
                searchModalInput.removeAttribute('aria-activedescendant');
                searchModalStatus.textContent = '';
                searchActiveIndex = -1;
                setInert([topbarEl, sidebarLeft, mainContainerEl, sidebarRightEl], true);
            }}

            function closeSearchModal() {{
                searchOverlay.classList.remove('open');
                searchOverlay.hidden = true;
                document.documentElement.style.overflow = '';
                setInert([topbarEl, sidebarLeft, mainContainerEl, sidebarRightEl], false);
                if (searchLastFocused && typeof searchLastFocused.focus === 'function') {{
                    searchLastFocused.focus();
                }}
            }}

            function isSearchModalOpen() {{
                return !searchOverlay.hidden;
            }}

            if (searchTrigger) {{
                searchTrigger.addEventListener('click', openSearchModal);
            }}
            var searchTriggerMobile = document.getElementById('search-trigger-mobile');
            if (searchTriggerMobile) {{
                searchTriggerMobile.addEventListener('click', openSearchModal);
            }}
            searchModalClose.addEventListener('click', closeSearchModal);
            searchOverlay.addEventListener('mousedown', function(e) {{
                if (e.target === searchOverlay) closeSearchModal();
            }});

            document.addEventListener('keydown', function(e) {{
                var isCtrlK = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k';
                if (isCtrlK) {{
                    e.preventDefault();
                    isSearchModalOpen() ? closeSearchModal() : openSearchModal();
                    return;
                }}
                if (!isSearchModalOpen()) return;
                if (e.key === 'Escape') {{
                    e.preventDefault();
                    closeSearchModal();
                    return;
                }}
                if (e.key === 'Tab') {{
                    var focusable = [searchModalInput, searchModalClose];
                    var currentIndex = focusable.indexOf(document.activeElement);
                    e.preventDefault();
                    var nextIndex = e.shiftKey
                        ? (currentIndex <= 0 ? focusable.length - 1 : currentIndex - 1)
                        : (currentIndex === -1 || currentIndex === focusable.length - 1 ? 0 : currentIndex + 1);
                    focusable[nextIndex].focus();
                    return;
                }}
                if (e.key === 'ArrowDown') {{
                    e.preventDefault();
                    if (getSearchResultItems().length) setSearchActiveIndex(searchActiveIndex + 1);
                    return;
                }}
                if (e.key === 'ArrowUp') {{
                    e.preventDefault();
                    if (getSearchResultItems().length) setSearchActiveIndex(searchActiveIndex - 1);
                    return;
                }}
                if (e.key === 'Enter') {{
                    var items = getSearchResultItems();
                    if (searchActiveIndex >= 0 && items[searchActiveIndex]) {{
                        e.preventDefault();
                        items[searchActiveIndex].click();
                    }}
                }}
            }});

            searchModalResults.addEventListener('mouseover', function(e) {{
                var item = e.target.closest('.search-result-item');
                if (!item) return;
                var index = getSearchResultItems().indexOf(item);
                if (index !== -1) setSearchActiveIndex(index);
            }});

            searchModalInput.addEventListener('input', function() {{
                clearTimeout(searchTimeout);
                var query = searchModalInput.value.trim();
                searchActiveIndex = -1;
                searchRequestId += 1;
                if (query.length < 2) {{
                    searchModalResults.innerHTML = '';
                    searchModalInput.setAttribute('aria-expanded', 'false');
                    searchModalStatus.textContent = '';
                    return;
                }}
                searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">{Localization.JsEncode(l.SearchSearching)}</div>';
                var requestId = searchRequestId;
                searchTimeout = setTimeout(function() {{
                    barkRunSearch(query)
                        .then(function(data) {{
                            if (requestId !== searchRequestId) return; // a newer keystroke superseded this request
                            if (data.length === 0) {{
                                searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">{Localization.JsEncode(l.SearchNoResults)}</div>';
                                searchModalStatus.textContent = '{Localization.JsEncode(l.SearchNoResults)}';
                            }} else {{
                                var terms = query.split(/\s+/).filter(Boolean);
                                var html = '';
                                data.forEach(function(r, i) {{
                                    html += '<a href=""{basePath}/' + escapeHtml(r.path) + '/"" class=""search-result-item"" role=""option"" id=""search-result-' + i + '"" aria-selected=""false"" tabindex=""-1"">' +
                                        '<div class=""search-result-title"">' + highlightMatches(r.title, terms) + '</div>' +
                                        (r.excerpt ? '<div class=""search-result-excerpt"">' + highlightMatches(r.excerpt, terms) + '</div>' : '') +
                                        '</a>';
                                }});
                                searchModalResults.innerHTML = html;
                                searchModalStatus.textContent = data.length + (data.length === 1 ? ' {Localization.JsEncode(l.SearchResultSingular)}' : ' {Localization.JsEncode(l.SearchResultPlural)}');
                            }}
                            searchModalInput.setAttribute('aria-expanded', 'true');
                        }})
                        ['catch'](function() {{
                            if (requestId !== searchRequestId) return;
                            searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">{Localization.JsEncode(l.SearchError)}</div>';
                            searchModalStatus.textContent = '{Localization.JsEncode(l.SearchFailed)}';
                        }});
                }}, 200);
            }});

            // Wrap wide tables in scroll container; overflow-x:auto on table itself doesn't reliably contain it.
            var tables = document.querySelectorAll('.content table');
            tables.forEach(function(table) {{
                var wrapper = document.createElement('div');
                wrapper.className = 'table-wrapper';
                table.parentNode.insertBefore(wrapper, table);
                wrapper.appendChild(table);
            }});


            var codeBlocks = document.querySelectorAll('.content pre');
            codeBlocks.forEach(function(pre) {{
                var wrapper = document.createElement('div');
                wrapper.className = 'code-block-wrapper';
                pre.parentNode.insertBefore(wrapper, pre);
                wrapper.appendChild(pre);

                var buttons = document.createElement('div');
                buttons.className = 'code-block-buttons';

                var iconCopy = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><rect width=""14"" height=""14"" x=""8"" y=""8"" rx=""2"" ry=""2""/><path d=""M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2""/></svg>';
                var iconCheck = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M20 6 9 17l-5-5""/></svg>';
                var iconX = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M18 6 6 18""/><path d=""m6 6 12 12""/></svg>';
                var iconDownload = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" x2=""12"" y1=""3"" y2=""15""/></svg>';
                var iconSpin = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" class=""spin"" aria-hidden=""true""><path d=""M21 12a9 9 0 1 1-6.219-8.56""/></svg>';

                // Diff-removed lines are rendered for reading, but must not land in the clipboard or the file.
                function codeText() {{
                    var lines = pre.querySelectorAll('code .line');
                    if (!lines.length) {{
                        var code = pre.querySelector('code');
                        return code ? code.textContent : pre.textContent;
                    }}
                    var kept = [];
                    lines.forEach(function(line) {{
                        if (!line.classList.contains('remove')) kept.push(line.textContent);
                    }});
                    return kept.join('\n');
                }}

                function flash(btn, base, icon, cls) {{
                    btn.innerHTML = icon;
                    btn.classList.add(cls);
                    setTimeout(function() {{
                        btn.innerHTML = base;
                        btn.classList.remove(cls);
                    }}, 2000);
                }}
                var iconResult = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z""/><circle cx=""12"" cy=""12"" r=""3""/></svg>';
                var iconDiff = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M12 3v6M9 6h6""/><path d=""M9 18h6""/><path d=""M4 12h16""/></svg>';

                var diffBlock = pre.closest('[class^=""language-""]');
                if (diffBlock && pre.querySelector('.line.diff.remove')) {{
                    var resultBtn = document.createElement('button');
                    resultBtn.innerHTML = iconResult;
                    resultBtn.setAttribute('aria-pressed', 'false');
                    resultBtn.setAttribute('aria-label', 'Show result');
                    resultBtn.setAttribute('title', 'Show result');
                    resultBtn.addEventListener('click', function() {{
                        var showing = diffBlock.classList.toggle('show-result');
                        resultBtn.innerHTML = showing ? iconDiff : iconResult;
                        resultBtn.setAttribute('aria-pressed', showing ? 'true' : 'false');
                        resultBtn.setAttribute('aria-label', showing ? 'Show diff' : 'Show result');
                        resultBtn.setAttribute('title', showing ? 'Show diff' : 'Show result');
                    }});
                    buttons.appendChild(resultBtn);
                }}

                var copyBtn = document.createElement('button');
                copyBtn.innerHTML = iconCopy;
                copyBtn.setAttribute('aria-label', 'Copy code');
                copyBtn.setAttribute('title', 'Copy code');
                copyBtn.addEventListener('click', function() {{
                    navigator.clipboard.writeText(codeText()).then(function() {{
                        flash(copyBtn, iconCopy, iconCheck, 'copied');
                    }})['catch'](function() {{
                        flash(copyBtn, iconCopy, iconX, 'failed');
                    }});
                }});
                buttons.appendChild(copyBtn);

                var downloadBtn = document.createElement('button');
                downloadBtn.innerHTML = iconDownload;
                downloadBtn.setAttribute('aria-label', 'Download code');
                downloadBtn.setAttribute('title', 'Download code');
                downloadBtn.addEventListener('click', function() {{
                    if (downloadBtn.disabled) return;
                    downloadBtn.disabled = true;
                    downloadBtn.innerHTML = iconSpin;
                    // Blob build is synchronous; yield once so the spinner paints before it runs.
                    setTimeout(function() {{
                        var url = URL.createObjectURL(new Blob([codeText()], {{ type: 'text/plain' }}));
                        var a = document.createElement('a');
                        a.href = url;
                        var langBlock = pre.closest('[class^=""language-""]');
                        a.download = (langBlock && langBlock.dataset.filename) || 'code.txt';
                        document.body.appendChild(a);
                        a.click();
                        document.body.removeChild(a);
                        URL.revokeObjectURL(url);
                        downloadBtn.disabled = false;
                        flash(downloadBtn, iconDownload, iconCheck, 'copied');
                    }}, 0);
                }});
                buttons.appendChild(downloadBtn);

                wrapper.appendChild(buttons);
            }});

            var codeGroups = document.querySelectorAll('.bark-code-group');
            codeGroups.forEach(function(group) {{
                var inputs = group.querySelectorAll('.tabs input');
                var labels = group.querySelectorAll('.tabs label');
                var blocks = group.querySelectorAll('.blocks > [class^=""language-""]');
                inputs.forEach(function(input, index) {{
                    input.addEventListener('change', function() {{
                        blocks.forEach(function(block, blockIndex) {{
                            block.classList.toggle('active', blockIndex === index);
                        }});
                        labels.forEach(function(label, labelIndex) {{
                            label.classList.toggle('active-tab', labelIndex === index);
                        }});
                    }});
                    if (input.checked) labels[index] && labels[index].classList.add('active-tab');
                }});
            }});

            // Mermaid ignores CSS variables, so the theme's tokens are read once and passed in.
            var mermaidBlocks = document.querySelectorAll('.mermaid');
            if (mermaidBlocks.length && window.mermaid) {{
                var currentTheme = document.documentElement.getAttribute('data-theme');
                var prefersDarkNow = window.matchMedia('(prefers-color-scheme: dark)').matches;
                var mermaidIsDark = currentTheme ? currentTheme === 'dark' : prefersDarkNow;
                var rootStyle = getComputedStyle(document.documentElement);
                function themeToken(name) {{ return rootStyle.getPropertyValue(name).trim(); }}
                window.mermaid.initialize({{
                    theme: 'base',
                    themeVariables: {{
                        darkMode: mermaidIsDark,
                        fontFamily: themeToken('--font-sans'),
                        background: themeToken('--bg-color'),
                        primaryColor: themeToken('--accent-light'),
                        primaryTextColor: themeToken('--text-color'),
                        primaryBorderColor: themeToken('--accent'),
                        secondaryColor: themeToken('--code-bg'),
                        tertiaryColor: themeToken('--sidebar-bg'),
                        mainBkg: themeToken('--accent-light'),
                        nodeBorder: themeToken('--accent'),
                        clusterBkg: themeToken('--sidebar-bg'),
                        clusterBorder: themeToken('--border'),
                        lineColor: themeToken('--text-muted'),
                        textColor: themeToken('--text-color'),
                        titleColor: themeToken('--text-color'),
                        edgeLabelBackground: themeToken('--bg-color'),
                        noteBkgColor: themeToken('--code-bg'),
                        noteTextColor: themeToken('--text-color'),
                        noteBorderColor: themeToken('--border'),
                        // Alert hues, not accent tints: slices derived from one accent are indistinguishable.
                        pie1: themeToken('--accent'),
                        pie2: themeToken('--alert-note'),
                        pie3: themeToken('--alert-tip'),
                        pie4: themeToken('--alert-warning'),
                        pie5: themeToken('--alert-important'),
                        pie6: themeToken('--alert-caution'),
                        pieStrokeColor: themeToken('--bg-color'),
                        pieOuterStrokeColor: themeToken('--border'),
                        pieSectionTextColor: themeToken('--bg-color'),
                        pieTitleTextColor: themeToken('--text-color'),
                        pieLegendTextColor: themeToken('--text-color')
                    }}
                }});
                window.mermaid.run();
            }}

            var localeToggle = document.getElementById('locale-toggle');
            var localeDropdown = document.getElementById('locale-dropdown');
            if (localeToggle && localeDropdown) {{
                function closeLocaleMenu() {{
                    localeDropdown.hidden = true;
                    localeToggle.setAttribute('aria-expanded', 'false');
                }}
                function openLocaleMenu() {{
                    localeDropdown.hidden = false;
                    localeToggle.setAttribute('aria-expanded', 'true');
                    var first = localeDropdown.querySelector('.locale-option--current') || localeDropdown.querySelector('.locale-option');
                    if (first) first.focus();
                }}
                localeToggle.addEventListener('click', function(e) {{
                    e.stopPropagation();
                    if (localeDropdown.hidden) openLocaleMenu(); else closeLocaleMenu();
                }});
                document.addEventListener('click', function(e) {{
                    if (!localeDropdown.hidden && !localeDropdown.contains(e.target) && e.target !== localeToggle)
                        closeLocaleMenu();
                }});
                document.addEventListener('keydown', function(e) {{
                    if (e.key === 'Escape' && !localeDropdown.hidden) {{ closeLocaleMenu(); localeToggle.focus(); }}
                }});
                localeDropdown.addEventListener('keydown', function(e) {{
                    if (e.key === 'Tab') {{ closeLocaleMenu(); return; }}
                    var items = Array.prototype.slice.call(localeDropdown.querySelectorAll('.locale-option'));
                    var idx = items.indexOf(document.activeElement);
                    if (e.key === 'ArrowDown') idx = (idx + 1) % items.length;
                    else if (e.key === 'ArrowUp') idx = (idx - 1 + items.length) % items.length;
                    else if (e.key === 'Home') idx = 0;
                    else if (e.key === 'End') idx = items.length - 1;
                    else return;
                    e.preventDefault();
                    items[idx].focus();
                }});
            }}

            var pageControlsToggle = document.querySelector('.page-controls-toggle');
            var pageControlsMenu = document.querySelector('.page-controls-menu');
            if (pageControlsToggle && pageControlsMenu) {{
                var pageControlsStatus = document.querySelector('[data-page-controls-status]');
                var pageControlsItems = Array.prototype.slice.call(pageControlsMenu.querySelectorAll('.page-controls-item'));

                function focusPageControlsItem(idx) {{
                    pageControlsItems.forEach(function(item, i) {{ item.tabIndex = i === idx ? 0 : -1; }});
                    if (pageControlsItems[idx]) pageControlsItems[idx].focus();
                }}
                function closePageControls(restoreFocus) {{
                    if (pageControlsMenu.hidden) return;
                    pageControlsMenu.hidden = true;
                    pageControlsToggle.setAttribute('aria-expanded', 'false');
                    if (restoreFocus) pageControlsToggle.focus();
                }}
                function openPageControls(focusLast) {{
                    pageControlsMenu.hidden = false;
                    pageControlsToggle.setAttribute('aria-expanded', 'true');
                    pageControlsMenu.style.right = '';
                    pageControlsMenu.style.left = '';
                    if (pageControlsMenu.getBoundingClientRect().left < 8) {{
                        pageControlsMenu.style.right = 'auto';
                        pageControlsMenu.style.left = '0';
                    }}
                    focusPageControlsItem(focusLast ? pageControlsItems.length - 1 : 0);
                }}
                pageControlsToggle.addEventListener('click', function(e) {{
                    e.stopPropagation();
                    if (!pageControlsMenu.hidden) {{ closePageControls(true); }} else {{ openPageControls(false); }}
                }});
                pageControlsToggle.addEventListener('keydown', function(e) {{
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {{
                        e.preventDefault();
                        openPageControls(e.key === 'ArrowUp');
                    }}
                }});
                document.addEventListener('click', function(e) {{
                    if (!pageControlsMenu.contains(e.target)) closePageControls(false);
                }});
                pageControlsMenu.addEventListener('keydown', function(e) {{
                    var idx = pageControlsItems.indexOf(document.activeElement);
                    var last = pageControlsItems.length - 1;
                    if (e.key === 'ArrowDown') {{ e.preventDefault(); focusPageControlsItem(idx >= last ? 0 : idx + 1); }}
                    else if (e.key === 'ArrowUp') {{ e.preventDefault(); focusPageControlsItem(idx <= 0 ? last : idx - 1); }}
                    else if (e.key === 'Home') {{ e.preventDefault(); focusPageControlsItem(0); }}
                    else if (e.key === 'End') {{ e.preventDefault(); focusPageControlsItem(last); }}
                    else if (e.key === 'Escape') {{ e.preventDefault(); closePageControls(true); }}
                    else if (e.key === 'Tab') {{ closePageControls(false); }}
                }});
                pageControlsMenu.querySelectorAll('a.page-controls-item').forEach(function(link) {{
                    link.addEventListener('click', function() {{ closePageControls(true); }});
                }});

                function announce(message) {{
                    if (!pageControlsStatus) return;
                    pageControlsStatus.textContent = '';
                    setTimeout(function() {{ pageControlsStatus.textContent = message; }}, 50);
                }}
                function showResult(btn, ok) {{
                    var label = btn.querySelector('.page-controls-label');
                    var saved = label.textContent;
                    var message = ok ? '{Localization.JsEncode(l.Copied)}' : '{Localization.JsEncode(l.CopyFailed)}';
                    label.textContent = message;
                    btn.classList.toggle('is-done', ok);
                    announce(message);
                    if (ok) setTimeout(function() {{ closePageControls(true); }}, 700);
                    setTimeout(function() {{ label.textContent = saved; btn.classList.remove('is-done'); }}, 2000);
                }}

                pageControlsMenu.querySelectorAll('[data-copy-url]').forEach(function(btn) {{
                    btn.addEventListener('click', function() {{
                        if (btn.getAttribute('aria-busy') === 'true') return;
                        btn.setAttribute('aria-busy', 'true');
                        fetch(btn.getAttribute('data-copy-url'))
                            .then(function(r) {{ if (!r.ok) throw new Error(); return r.text(); }})
                            .then(function(text) {{ return navigator.clipboard.writeText(text); }})
                            .then(function() {{ btn.removeAttribute('aria-busy'); showResult(btn, true); }})
                            ['catch'](function() {{ btn.removeAttribute('aria-busy'); showResult(btn, false); }});
                    }});
                }});

                pageControlsMenu.querySelectorAll('[data-copy-value]').forEach(function(btn) {{
                    btn.addEventListener('click', function() {{
                        var value = new URL(btn.getAttribute('data-copy-value'), window.location.href).href;
                        navigator.clipboard.writeText(value)
                            .then(function() {{ showResult(btn, true); }})
                            ['catch'](function() {{ showResult(btn, false); }});
                    }});
                }});
            }}

            var promoBar = document.getElementById('promo-bar');
            var promoClose = document.getElementById('promo-bar-close');
            if (promoBar && promoClose) {{
                promoClose.addEventListener('click', function() {{
                    try {{ localStorage.setItem('bark-promo-dismissed', promoBar.getAttribute('data-promo-id') || '1'); }} catch (_) {{}}
                    var removed = false;
                    var removeBar = function() {{ if (!removed) {{ removed = true; promoBar.remove(); }} }};
                    promoBar.addEventListener('transitionend', function(e) {{
                        if (e.propertyName === 'grid-template-rows') removeBar();
                    }});
                    setTimeout(removeBar, 400);
                    promoBar.classList.add('promo-bar-hiding');
                }});
            }}

            document.querySelectorAll('.top-nav-item.has-dropdown').forEach(function(item) {{
                var btn = item.querySelector('.top-nav-link[aria-expanded]');
                if (!btn) return;
                item.addEventListener('focusin', function() {{
                    btn.setAttribute('aria-expanded', 'true');
                }});
                item.addEventListener('focusout', function() {{
                    setTimeout(function() {{
                        if (!item.matches(':focus-within')) btn.setAttribute('aria-expanded', 'false');
                    }}, 0);
                }});
                item.addEventListener('mouseenter', function() {{ btn.setAttribute('aria-expanded', 'true'); }});
                item.addEventListener('mouseleave', function() {{ btn.setAttribute('aria-expanded', 'false'); }});
            }});

            function positionAbbrTip(e) {{
                var abbr = e.target.closest && e.target.closest('abbr[data-tip]');
                if (!abbr) return;
                var vw = document.documentElement.clientWidth || window.innerWidth;
                var half = Math.min(120, vw * 0.3);
                var rect = abbr.getBoundingClientRect();
                var center = rect.left + rect.width / 2;
                var shift = 0;
                if (center - half < 8) shift = 8 - (center - half);
                else if (center + half > vw - 8) shift = (vw - 8) - (center + half);
                abbr.style.setProperty('--tip-shift', shift + 'px');
            }}
            document.addEventListener('mouseover', positionAbbrTip);
            document.addEventListener('focusin', positionAbbrTip);
        }});
");
    }

    private static string MinifyJs(string js)
    {
        var lines = js.Split('\n');
        var kept = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                continue;
            kept.Add(trimmed);
        }
        return string.Join('\n', kept);
    }
}
