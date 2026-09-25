namespace Bark.Services.Layout;

public static partial class LayoutProvider
{
    private static GeneratedAsset? _cssAsset;

    private static string GetStylesLink(string themeTokenCss, string themeComponentCss, string basePath)
    {
        var asset = GetStylesAsset(themeTokenCss, themeComponentCss, basePath);
        return $"<link rel=\"stylesheet\" href=\"{basePath}/bark.css?v={asset.Version}\">";
    }

    internal static GeneratedAsset GetStylesAsset(string themeTokenCss, string themeComponentCss, string basePath) =>
        GetOrBuildAsset(ref _cssAsset, themeTokenCss + " " + themeComponentCss + " " + basePath,
            () => MinifyCss($@"
{themeTokenCss}        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }}
        html, body {{
            overflow-x: clip;
        }}
        html {{
            accent-color: var(--accent);
            caret-color: var(--accent);
            -webkit-text-size-adjust: 100%;
            text-size-adjust: 100%;
            scrollbar-color: color-mix(in oklab, var(--text-muted) 45%, transparent) transparent;
        }}
        ::selection {{
            background-color: var(--selection);
            color: var(--text-color);
        }}
        body {{
            display: flex;
            flex-direction: column;
            min-height: 100dvh;
            font-family: var(--font-sans);
            background-color: var(--bg-color);
            color: var(--text-color);
            line-height: 1.6;
            -webkit-font-smoothing: antialiased;
            transition: background-color 0.15s ease, color 0.15s ease;
        }}
        #scroll-indicator {{
            position: fixed;
            top: 0;
            left: 0;
            height: 3px;
            background-color: var(--accent);
            width: 100%;
            z-index: 1101;
            transform: scaleX(0);
            transform-origin: left;
            transition: transform 0.15s ease;
        }}
        :focus-visible {{
            outline: 2px solid var(--accent);
            outline-offset: 2px;
        }}
        .sidebar-left :focus-visible,
        .sidebar-right :focus-visible {{
            outline-offset: -2px;
        }}
        .skip-link {{
            position: absolute;
            top: 0;
            left: 0;
            z-index: 1100;
            width: 1px;
            height: 1px;
            overflow: hidden;
            clip-path: inset(50%);
            white-space: nowrap;
            background: var(--accent);
            color: var(--bg-color);
            padding: 0.75rem 1.25rem;
            border-radius: 0 0 6px 0;
            text-decoration: none;
            font-size: 0.9rem;
        }}
        .skip-link:focus {{
            width: auto;
            height: auto;
            overflow: visible;
            clip-path: none;
            white-space: normal;
        }}
        .no-theme-transition, .no-theme-transition * {{
            transition: none !important;
        }}
        :root {{
            --topbar-height: 57px;
            --drawer-top: var(--topbar-height);
        }}
        .icon-btn {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 36px;
            height: 36px;
            border-radius: 6px;
            border: none;
            background: transparent;
            color: var(--text-muted);
            cursor: pointer;
            flex-shrink: 0;
            text-decoration: none;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .icon-btn:hover {{
            color: var(--accent);
            background-color: var(--code-bg);
        }}
        .icon-btn svg {{
            width: 18px;
            height: 18px;
        }}
        .promo-bar {{
            display: grid;
            grid-template-rows: 1fr;
            background-color: var(--promo-bg);
            color: var(--promo-text);
            box-shadow: inset 0 -1px 0 color-mix(in oklab, var(--promo-text) 14%, transparent);
            font-size: 0.875rem;
            line-height: 1.4;
            text-align: center;
            transition: grid-template-rows 0.22s cubic-bezier(0.22, 1, 0.36, 1), opacity 0.18s ease-out;
        }}
        .promo-bar-inner {{
            position: relative;
            overflow: hidden;
            min-height: 0;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .promo-bar-content {{
            padding: 0.5rem 3rem;
        }}
        .promo-bar-content p {{
            margin: 0;
            display: inline;
        }}
        .promo-bar-content a {{
            color: inherit;
            text-decoration: underline;
            text-underline-offset: 2px;
            font-weight: 600;
        }}
        .promo-bar-content a:hover {{
            text-decoration-thickness: 2px;
        }}
        .promo-bar-content code {{
            background-color: color-mix(in oklab, var(--promo-text) 16%, transparent);
            color: inherit;
            padding: 0.1em 0.35em;
            border-radius: 4px;
            font-family: var(--font-mono);
            font-size: 0.85em;
        }}
        .promo-bar-close {{
            position: absolute;
            right: 0.75rem;
            top: 50%;
            transform: translateY(-50%);
            color: inherit;
            opacity: 0.75;
            transition: opacity 0.15s ease, background-color 0.15s ease;
        }}
        .promo-bar-close:hover {{
            color: inherit;
            opacity: 1;
            background-color: color-mix(in oklab, var(--promo-text) 15%, transparent);
        }}
        .promo-bar-close:focus-visible {{
            outline-color: var(--promo-text);
            opacity: 1;
        }}
        .promo-bar.promo-bar-hiding {{
            grid-template-rows: 0fr;
            opacity: 0;
        }}
        .promo-dismissed .promo-bar {{
            display: none;
        }}
        .topbar {{
            height: var(--topbar-height);
            background-color: var(--bg-color);
            border-bottom: 1px solid var(--border);
            position: sticky;
            top: 0;
            z-index: 1002;
        }}
        .topbar-inner {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            height: 100%;
            max-width: 1600px;
            margin: 0 auto;
            padding: 0 1.5rem;
            box-sizing: border-box;
            position: relative;
        }}
        .topbar-left {{
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }}
        .topbar-right {{
            display: flex;
            align-items: center;
            gap: 0.25rem;
        }}
        .top-nav {{
            position: absolute;
            left: 50%;
            transform: translateX(-50%);
            display: flex;
            align-items: center;
            gap: 1.5rem;
            height: 100%;
        }}
        .top-nav-item {{
            display: flex;
            align-items: center;
            height: 100%;
            position: relative;
        }}
        .top-nav-link {{
            display: inline-flex;
            align-items: center;
            gap: 0.3rem;
            font-size: 0.9rem;
            font-weight: 500;
            color: var(--text-muted);
            text-decoration: none;
            background: none;
            border: none;
            cursor: pointer;
            padding: 0;
            font-family: inherit;
        }}
        .top-nav-link:hover {{
            color: var(--accent);
        }}
        .top-nav-link.active {{
            color: var(--text-color);
            font-weight: 600;
        }}
        .top-nav-label {{
            display: inline-grid;
        }}
        .top-nav-label::before {{
            content: attr(data-label);
            font-weight: 600;
            height: 0;
            visibility: hidden;
        }}
        .top-nav-chevron {{
            width: 14px;
            height: 14px;
            transition: transform 0.15s ease;
        }}
        .top-nav-item.has-dropdown:hover .top-nav-chevron,
        .top-nav-item.has-dropdown:focus-within .top-nav-chevron {{
            transform: rotate(180deg);
        }}
        .top-nav-dropdown-menu {{
            display: none;
            position: absolute;
            top: 100%;
            left: 0;
            min-width: 180px;
            background-color: var(--bg-color);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 0.4rem;
            box-shadow: var(--shadow-md);
            z-index: 1003;
        }}
        .top-nav-item.has-dropdown:hover .top-nav-dropdown-menu,
        .top-nav-item.has-dropdown:focus-within .top-nav-dropdown-menu {{
            display: block;
        }}
        .top-nav-dropdown-link {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 0.5rem;
            padding: 0.45rem 0.6rem;
            border-radius: 6px;
            font-size: 0.875rem;
            color: var(--text-color);
            text-decoration: none;
        }}
        .top-nav-dropdown-link:hover {{
            background-color: var(--code-bg);
            color: var(--accent);
        }}
        .external-link-icon {{
            display: inline-block;
            width: 12px;
            height: 12px;
            flex-shrink: 0;
            opacity: 0.6;
            vertical-align: -1px;
            margin-left: 0.25rem;
        }}
        .mobile-top-nav {{
            display: none;
        }}
        .layout {{
            display: grid;
            grid-template-columns: 18rem minmax(0, 1fr);
            grid-template-rows: 1fr auto;
            width: 100%;
            max-width: 1600px;
            margin: 0 auto;
            padding: 0 2rem;
            box-sizing: border-box;
            flex: 1 0 auto;
        }}
        .layout.no-left-sidebar {{
            grid-template-columns: minmax(0, 1fr);
        }}
        .content-shell {{
            display: flex;
            min-width: 0;
        }}
        @media (min-width: 769px) {{
            .layout.no-left-sidebar > .sidebar-left {{
                display: none;
            }}
        }}
        .sidebar-left {{
            padding: 2.75rem 1rem 2.75rem 0;
            box-sizing: border-box;
            grid-row: 1 / -1;
            position: sticky;
            top: var(--topbar-height);
            align-self: start;
            height: calc(100vh - var(--topbar-height));
            overflow-y: auto;
        }}
        .brand a {{
            font-size: 1.1rem;
            font-weight: 600;
            letter-spacing: -0.02em;
            color: var(--text-color);
            text-decoration: none;
        }}
        .brand a:hover {{
            color: var(--accent);
        }}
        .brand img {{
            height: 22px;
            width: auto;
            vertical-align: middle;
            margin-right: 0.75rem;
        }}
        .theme-toggle .icon-moon {{
            display: none;
        }}
        :root[data-theme=""dark""] .theme-toggle .icon-sun {{
            display: none;
        }}
        :root[data-theme=""dark""] .theme-toggle .icon-moon {{
            display: block;
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .theme-toggle .icon-sun {{
                display: none;
            }}
            :root:not([data-theme=""light""]) .theme-toggle .icon-moon {{
                display: block;
            }}
        }}
        .sr-only {{
            position: absolute;
            width: 1px;
            height: 1px;
            padding: 0;
            margin: -1px;
            overflow: hidden;
            clip: rect(0, 0, 0, 0);
            white-space: nowrap;
            border: 0;
        }}
        .search-trigger {{
            display: flex;
            align-items: center;
            gap: 0.55rem;
            margin-left: 1rem;
            padding: 0.4rem 0.65rem;
            border: 1px solid var(--search-border);
            border-radius: 8px;
            background-color: var(--search-bg);
            color: var(--text-muted);
            font-family: inherit;
            font-size: 0.85rem;
            cursor: pointer;
            transition: border-color 0.15s ease, color 0.15s ease;
        }}
        .search-trigger-mobile {{
            display: none;
        }}
        .search-trigger:hover {{
            border-color: var(--search-hover-border);
            color: var(--text-color);
        }}
        .search-trigger svg {{
            width: 16px;
            height: 16px;
            flex-shrink: 0;
        }}
        .search-trigger-kbd {{
            font-family: var(--font-sans);
            font-size: 0.7rem;
            font-weight: 400;
            letter-spacing: 0.02em;
            border: 1px solid var(--border);
            border-radius: 4px;
            padding: 0.1rem 0.35rem;
            background-color: var(--bg-color);
            color: var(--text-muted);
            pointer-events: none;
            user-select: none;
            opacity: 0;
            transition: opacity 0.15s ease;
        }}
        .search-trigger:hover .search-trigger-kbd,
        .search-trigger:focus-visible .search-trigger-kbd {{
            opacity: 1;
        }}
        .search-overlay {{
            position: fixed;
            inset: 0;
            z-index: 1200;
            background-color: var(--overlay-bg);
            display: flex;
            align-items: flex-start;
            justify-content: center;
            padding: 8vh 1rem 2rem;
            opacity: 0;
            transition: opacity 0.15s ease;
        }}
        .search-overlay[hidden] {{
            display: none;
        }}
        .search-overlay.open {{
            opacity: 1;
        }}
        .search-modal {{
            width: 100%;
            max-width: 720px;
            max-height: 80vh;
            background-color: var(--bg-color);
            border: 1px solid var(--border);
            border-radius: 12px;
            box-shadow: var(--shadow-lg);
            display: flex;
            flex-direction: column;
            overflow: hidden;
            transform: translateY(-12px) scale(0.98);
            transition: transform 0.15s ease;
        }}
        .search-overlay.open .search-modal {{
            transform: translateY(0) scale(1);
        }}
        .search-modal-header {{
            display: flex;
            align-items: center;
            gap: 0.75rem;
            padding: 1rem 1.25rem;
            border-bottom: 1px solid var(--border);
            flex-shrink: 0;
        }}
        .search-modal-header > svg {{
            width: 20px;
            height: 20px;
            color: var(--text-muted);
            flex-shrink: 0;
        }}
        .search-modal-input {{
            flex: 1;
            min-width: 0;
            border: none;
            outline: none;
            background: transparent;
            color: var(--text-color);
            font-size: 1.05rem;
            font-family: var(--font-sans);
        }}
        .search-modal-close {{
            flex-shrink: 0;
        }}
        .search-modal-results {{
            flex: 1;
            overflow-y: auto;
            padding: 0.5rem;
        }}
        .search-modal-results:empty {{
            display: none;
        }}
        .search-result-item {{
            display: block;
            padding: 0.7rem 0.9rem;
            border-radius: 8px;
            text-decoration: none;
            transition: background-color 0.1s ease;
        }}
        .search-result-item.active, .search-result-item:hover {{
            background-color: var(--accent-light);
        }}
        .search-result-title {{
            font-weight: 500;
            color: var(--text-color);
            font-size: 0.9rem;
        }}
        .search-result-excerpt {{
            font-size: 0.8rem;
            color: var(--text-muted);
            margin-top: 0.2rem;
        }}
        .search-highlight {{
            background-color: var(--accent-light);
            color: var(--accent);
            border-radius: 3px;
            padding: 0 0.15em;
            font-weight: 600;
        }}
        .search-result-empty {{
            color: var(--text-muted);
            font-size: 0.85rem;
            padding: 1rem;
            text-align: center;
        }}
        .DocSearch-Commands {{
            display: flex;
            gap: 1.25rem;
            padding: 0.6rem 1.25rem;
            margin: 0;
            list-style: none;
            border-top: 1px solid var(--border);
            font-size: 0.75rem;
            color: var(--text-muted);
            flex-shrink: 0;
        }}
        .DocSearch-Commands li {{
            display: flex;
            align-items: center;
            gap: 0.4rem;
        }}
        .DocSearch-Commands-Key {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            font-family: var(--font-mono);
            border: 1px solid var(--border);
            border-radius: 4px;
            padding: 0.1rem 0.3rem;
            background-color: var(--code-bg);
            min-width: 1.4rem;
            height: 1.4rem;
        }}
        .DocSearch-Commands-Key svg {{
            width: 14px;
            height: 14px;
        }}
        .DocSearch-Escape-Key {{
            font-size: 0.7rem;
            line-height: 1;
        }}
        @media (max-width: 768px) {{
            .topbar-left .search-trigger {{
                display: none;
            }}
            .search-trigger-mobile {{
                display: inline-flex;
            }}
            .search-modal-close {{
                width: 44px;
                height: 44px;
            }}
            .search-overlay {{
                padding: 0;
            }}
            .search-modal {{
                max-width: 100%;
                max-height: 100%;
                height: 100%;
                height: 100dvh;
                border-radius: 0;
            }}
            .DocSearch-Commands {{
                flex-wrap: wrap;
                row-gap: 0.4rem;
            }}
        }}
        .nav-group {{
            margin-bottom: 2.25rem;
        }}
        .nav-group-title {{
            font-size: 0.75rem;
            letter-spacing: 0.05em;
            color: var(--text-muted);
            margin-bottom: 1rem;
            font-weight: 600;
        }}
        .nav-list {{
            list-style: none;
        }}
        .nav-item a {{
            display: block;
            padding: 0.55rem 0.8rem;
            line-height: 1.4;
            color: var(--text-muted);
            text-decoration: none;
            font-size: 0.9rem;
            border-radius: 6px;
            margin-left: -0.8rem;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .nav-item a:hover {{
            color: var(--text-color);
            background-color: var(--nav-hover-bg);
        }}
        .nav-item.active a {{
            color: var(--accent);
            background-color: var(--nav-active-bg);
            font-weight: 500;
        }}
        .sidebar-tree {{
            font-size: 0.9rem;
        }}
        .sidebar-group {{
            margin-bottom: 0.25rem;
        }}
        .sidebar-group-summary {{
            display: block;
            list-style: none;
            cursor: pointer;
        }}
        .sidebar-group-summary::-webkit-details-marker {{
            display: none;
        }}
        .sidebar-group-summary::marker {{
            content: """";
        }}
        .sidebar-group.no-caret > .sidebar-group-title {{
            cursor: default;
        }}
        .sidebar-group-title {{
            display: flex;
            align-items: center;
            gap: 0.4rem;
            padding: 0.5rem 1rem;
            border-radius: 6px;
            user-select: none;
            transition: background-color 0.15s ease;
        }}
        .sidebar-group-summary:hover .sidebar-group-title {{
            background-color: var(--code-bg);
        }}
        .sidebar-group-title h2, .sidebar-group-title h3 {{
            font-size: 0.875rem;
            line-height: 1.4;
            color: var(--text-color);
            font-weight: 600;
            flex: 1;
            margin: 0;
        }}
        .caret-icon {{
            display: inline-flex;
            flex-shrink: 0;
            width: 16px;
            height: 16px;
            color: var(--text-muted);
            transition: transform 0.2s ease;
        }}
        .caret-icon svg {{
            width: 100%;
            height: 100%;
        }}
        details[open] > .sidebar-group-summary .caret-icon {{
            transform: rotate(90deg);
        }}
        .sidebar-group-items {{
            padding-left: 0.9rem;
            margin-bottom: 0.5rem;
        }}
        .sidebar-tree > .sidebar-group > .sidebar-group-items {{
            padding-left: 0;
        }}
        .sidebar-link {{
            margin-bottom: 0.1rem;
        }}
        .sidebar-tree > .sidebar-group + .sidebar-group,
        .sidebar-tree > .sidebar-group + .sidebar-link,
        .sidebar-tree > .sidebar-link + .sidebar-group,
        .sidebar-tree > .sidebar-link + .sidebar-link {{
            border-top: 1px solid var(--border);
            padding-top: 0.75rem;
            margin-top: 0.75rem;
        }}
        .sidebar-link a {{
            display: block;
            padding: 0.5rem 1rem;
            line-height: 1.4;
            color: var(--text-muted);
            text-decoration: none;
            font-size: 0.875rem;
            border-radius: 6px;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .sidebar-link a:hover {{
            color: var(--text-color);
            background-color: var(--nav-hover-bg);
        }}
        .sidebar-link.is-active a {{
            color: var(--accent);
            background-color: var(--nav-active-bg);
            font-weight: 500;
        }}
        .main-column {{
            display: flex;
            flex-direction: column;
            flex: 1 1 auto;
            max-width: 960px;
            min-width: 0;
        }}
        .toc-collapsed .main-column {{
            max-width: 1200px;
        }}
        .main-container {{
            padding: 3rem 3rem 3rem 4rem;
            flex: 1 0 auto;
            min-width: 0;
        }}
        .bark-home-layout {{
            grid-template-columns: 1fr;
        }}
        .bark-home-layout .sidebar-left {{
            display: none;
        }}
        .bark-home-layout .main-column {{
            max-width: 100%;
        }}
        .bark-home-layout .main-container {{
            max-width: 100%;
            padding: 0;
            display: flex;
            flex-direction: column;
        }}
        .bark-home-content {{
            max-width: 960px;
            margin: 0 auto;
            padding: 0 2rem;
            flex: 1;
            display: flex;
            flex-direction: column;
            width: 100%;
        }}
        .bark-home-content .content-footer {{
            margin-top: auto;
        }}
        .bark-hero {{
            text-align: center;
            padding: 4.5rem 1.5rem 5rem;
        }}
        .bark-hero-image {{
            font-size: 4rem;
            margin-bottom: 1.5rem;
            line-height: 1;
        }}
        .bark-hero-image img {{
            max-width: 200px;
            max-height: 200px;
        }}
        .bark-hero-name {{
            font-size: 3rem;
            font-weight: 700;
            color: var(--accent);
            letter-spacing: -0.04em;
            line-height: 1.05;
            margin-bottom: 0;
            text-wrap: balance;
        }}
        .bark-hero-name:has(+ .bark-hero-text) {{
            margin-bottom: 0.1em;
        }}
        .bark-hero-text {{
            font-size: 3rem;
            font-weight: 700;
            color: var(--text-color);
            letter-spacing: -0.04em;
            line-height: 1.05;
            margin-bottom: 1.1rem;
        }}
        .bark-hero-tagline {{
            font-size: 1.15rem;
            color: var(--text-muted);
            max-width: 540px;
            margin: 0 auto 2.75rem;
        }}
        @media (min-width: 1500px) {{
            .bark-home-content {{
                max-width: 1180px;
            }}
            .bark-hero {{
                padding: 7rem 1.5rem 6.5rem;
            }}
            .bark-hero-name,
            .bark-hero-text {{
                font-size: 3.5rem;
            }}
            .bark-hero-tagline {{
                font-size: 1.25rem;
                max-width: 620px;
                margin-bottom: 3.25rem;
            }}
        }}
        .bark-hero-actions {{
            display: flex;
            justify-content: center;
            gap: 0.9rem;
            flex-wrap: wrap;
        }}
        .bark-hero-action {{
            display: inline-flex;
            align-items: center;
            padding: 0.65rem 1.4rem;
            border-radius: 8px;
            font-weight: 600;
            font-size: 0.95rem;
            text-decoration: none;
            transition: opacity 0.15s ease, background-color 0.15s ease;
        }}
        .bark-hero-action.brand {{
            background-color: var(--accent);
            color: var(--bg-color);
        }}
        .bark-hero-action.brand:hover {{
            opacity: 0.85;
        }}
        .bark-hero-action.alt {{
            border: 1px solid var(--border);
            color: var(--text-color);
            background: transparent;
        }}
        .bark-hero-action.alt:hover {{
            background-color: var(--accent-light);
        }}
        .bark-features {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
            gap: 2.5rem 3rem;
            margin: 0 0 3rem;
        }}
        .bark-feature {{
            display: grid;
            grid-template-columns: auto 1fr;
            column-gap: 0.6rem;
            align-content: start;
            padding-top: 1.25rem;
            border-top: 1px solid var(--border);
            text-decoration: none;
            color: inherit;
        }}
        a.bark-feature:hover .bark-feature-title {{
            color: var(--accent);
        }}
        a.bark-feature:hover .bark-feature-icon {{
            color: var(--accent);
        }}
        .bark-feature-icon {{
            grid-column: 1;
            grid-row: 1;
            display: inline-flex;
            align-items: center;
            font-size: 1.15rem;
            color: var(--text-muted);
            background: none;
            transition: color 0.15s ease;
        }}
        .bark-feature-icon img {{
            width: 1.15rem;
            height: 1.15rem;
            object-fit: contain;
        }}
        .bark-feature-icon svg {{
            width: 1.15rem;
            height: 1.15rem;
            stroke: currentColor;
            stroke-width: 1.5;
            fill: none;
        }}
        .bark-icon-dark {{
            display: none;
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .bark-icon-light {{
                display: none;
            }}
            :root:not([data-theme=""light""]) .bark-icon-dark  {{
                display: inline;
            }}
        }}
        :root[data-theme=""dark""] .bark-icon-light {{
            display: none;
        }}
        :root[data-theme=""dark""] .bark-icon-dark  {{
            display: inline;
        }}
        .bark-feature-title {{
            grid-column: 2;
            grid-row: 1;
            font-size: 1rem;
            font-weight: 650;
            margin: 0;
            transition: color 0.15s ease;
        }}
        .bark-feature-details {{
            grid-column: 1 / -1;
            grid-row: 2;
            margin-top: 0.6rem;
            font-size: 0.9rem;
            color: var(--text-muted);
            line-height: 1.55;
        }}
        .page-controls {{
            position: relative;
            margin-left: auto;
            flex-shrink: 0;
        }}
        .page-controls-toggle {{
            color: var(--text-muted);
        }}
        .page-controls-toggle:hover,
        .page-controls-toggle[aria-expanded=""true""] {{
            color: var(--text-color);
            background-color: var(--code-bg);
        }}
        .page-controls-menu, .locale-dropdown {{
            position: absolute;
            top: calc(100% + 6px);
            right: 0;
            z-index: 200;
            display: flex;
            flex-direction: column;
            gap: 1px;
            min-width: 13.5rem;
            max-width: calc(100vw - 2rem);
            padding: 0.3rem;
            background: var(--bg-color);
            border: 1px solid var(--border);
            border-radius: 10px;
            box-shadow: var(--shadow-md);
            transform-origin: top right;
            animation: page-controls-in 0.14s ease-out;
        }}
        @keyframes page-controls-in {{
            from {{
                opacity: 0;
                transform: translateY(-4px) scale(0.98);
            }}
        }}
        .page-controls-menu[hidden], .locale-dropdown[hidden] {{
            display: none;
        }}
        .page-controls-menu .page-controls-item, .locale-dropdown .locale-option {{
            display: flex;
            align-items: center;
            gap: 0.6rem;
            width: 100%;
            min-height: 34px;
            padding: 0.4rem 0.65rem;
            box-sizing: border-box;
            border: none;
            border-radius: 6px;
            background: none;
            font: inherit;
            font-size: 0.875rem;
            line-height: 1.3;
            text-align: left;
            color: var(--text-color);
            text-decoration: none;
            white-space: nowrap;
            cursor: pointer;
            transition: background-color 0.12s ease, color 0.12s ease;
        }}
        .page-controls-item svg {{
            width: 16px;
            height: 16px;
            flex-shrink: 0;
            color: var(--text-muted);
            transition: color 0.12s ease;
        }}
        .page-controls-menu .page-controls-item:hover,
        .page-controls-menu .page-controls-item:focus-visible,
        .locale-dropdown .locale-option:hover,
        .locale-dropdown .locale-option:focus-visible {{
            background: var(--nav-hover-bg);
            color: var(--text-color);
        }}
        .page-controls-item:focus-visible, .locale-option:focus-visible {{
            outline: 2px solid var(--accent);
            outline-offset: -2px;
        }}
        .page-controls-item:hover svg,
        .page-controls-item:focus-visible svg {{
            color: var(--accent);
        }}
        .page-controls-item .page-controls-external {{
            width: 12px;
            height: 12px;
            margin-left: auto;
            opacity: 0.7;
        }}
        .page-controls-item[aria-busy=""true""] {{
            opacity: 0.6;
            cursor: progress;
        }}
        .page-controls-item.is-done svg {{
            color: var(--alert-tip);
        }}
        .page-controls-divider {{
            height: 1px;
            background: var(--border);
            margin: 0.25rem 0.35rem;
        }}
        @media (hover: none) and (pointer: coarse) {{
            .page-controls-menu .page-controls-item, .locale-dropdown .locale-option {{
                min-height: 44px;
            }}
        }}
        .locale-switcher {{
            position: relative;
        }}
        .repo-widget {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 36px;
            height: 36px;
            border-radius: 6px;
            color: var(--text-muted);
            text-decoration: none;
            flex-shrink: 0;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .repo-widget:hover {{
            color: var(--accent);
            background-color: var(--code-bg);
        }}
        .repo-widget svg {{
            width: 18px;
            height: 18px;
            flex-shrink: 0;
        }}
        .locale-dropdown {{
            min-width: 11rem;
        }}
        .locale-dropdown .locale-option--current {{
            font-weight: 600;
        }}
        .locale-option--current::after {{
            content: ""\2713"" / """";
            margin-left: auto;
            color: var(--accent);
        }}
        .translation-notice {{
            margin: 0 0 1.5rem;
            padding: 0.75rem 1rem;
            border-radius: 8px;
            background-color: var(--accent-light);
            font-size: 0.9rem;
            color: var(--text-muted);
        }}
        .translation-notice a {{
            color: var(--accent);
            font-weight: 500;
        }}
        .translation-notice--stale, .translation-notice--machine {{
            border-left-color: var(--alert-warning);
        }}
        .page-meta {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 1rem;
            margin-top: 5rem;
            padding-bottom: 1.125rem;
            flex-wrap: wrap;
        }}
        .page-meta + .pagination {{
            margin-top: 0;
        }}
        .page-meta-right {{
            margin-left: auto;
        }}
        .last-updated {{
            font-size: 0.875rem;
            color: var(--text-muted);
        }}
        .content a.edit-link {{
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
            font-size: 0.875rem;
            color: var(--text-muted);
            text-decoration: none;
            transition: color 0.15s ease;
        }}
        .content a.edit-link:hover {{
            color: var(--accent);
        }}
        .breadcrumb {{
            display: flex;
            align-items: center;
            gap: 0.4rem;
            margin-bottom: 1.5rem;
            font-size: 0.8rem;
            flex-wrap: wrap;
        }}
        .breadcrumb a {{
            color: var(--text-muted);
            text-decoration: none;
            transition: color 0.15s ease;
        }}
        .breadcrumb a:hover {{
            color: var(--accent);
        }}
        .breadcrumb .separator {{
            color: var(--text-muted);
        }}
        .breadcrumb .crumb-text {{
            color: var(--text-muted);
        }}
        .breadcrumb .current {{
            color: var(--text-color);
            font-weight: 500;
        }}
        .content h1:not(.bark-hero-text):not(.bark-hero-name) {{
            font-size: 2.35rem;
            font-weight: 650;
            letter-spacing: -0.035em;
            margin-bottom: 1rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h2, .content h3, .content h4, .content h5, .content h6 {{
            position: relative;
        }}
        .content h1:target, .content h2:target, .content h3:target,
        .content h4:target, .content h5:target, .content h6:target {{
            animation: bark-target-flash 2s ease-out;
        }}
        .content a.footnote-ref:target,
        .content a.footnote-back-ref:target {{
            background-color: var(--accent-light);
            outline: 2px solid var(--accent);
            border-radius: 4px;
            padding: 0 0.2em;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content .footnotes li:target {{
            background-color: var(--accent-light);
            outline: 2px solid var(--accent);
            border-radius: 6px;
            padding: 0.25rem 0.6rem;
            margin-left: -0.6rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content abbr[data-tip] {{
            position: relative;
            text-decoration: underline dotted var(--text-muted);
            text-decoration-thickness: 1px;
            text-underline-offset: 0.2em;
            cursor: help;
            -webkit-tap-highlight-color: transparent;
        }}
        .content abbr[data-tip]:focus {{
            outline: none;
        }}
        .content abbr[data-tip]:focus-visible {{
            outline: 2px solid var(--accent);
            outline-offset: 2px;
            border-radius: 3px;
        }}
        .content abbr[data-tip]::after {{
            content: attr(data-tip);
            position: absolute;
            left: 50%;
            bottom: calc(100% + 0.45rem);
            transform: translateX(calc(-50% + var(--tip-shift, 0px))) translateY(0.2rem);
            z-index: 20;
            width: max-content;
            max-width: min(15rem, 60vw);
            padding: 0.4rem 0.6rem;
            background-color: var(--sidebar-bg);
            color: var(--text-color);
            border: 1px solid var(--border);
            border-radius: 6px;
            box-shadow: var(--shadow-md);
            font: 400 0.8rem/1.4 var(--font-sans);
            text-align: center;
            text-decoration: none;
            white-space: normal;
            opacity: 0;
            visibility: hidden;
            pointer-events: none;
            transition: opacity 0.12s ease, transform 0.12s ease, visibility 0.12s;
        }}
        .content abbr[data-tip]:hover::after,
        .content abbr[data-tip]:focus::after {{
            opacity: 1;
            visibility: visible;
            transform: translateX(calc(-50% + var(--tip-shift, 0px))) translateY(0);
        }}
        @media (prefers-reduced-motion: reduce) {{
            .content abbr[data-tip]::after {{
                transition: none;
            }}
        }}
        @keyframes bark-target-flash {{
            0%, 40% {{
                background-color: var(--accent-light);
            }}
            100% {{
                background-color: transparent;
            }}
        }}
        @media (prefers-reduced-motion: reduce) {{
            .content h1:target, .content h2:target, .content h3:target,
            .content h4:target, .content h5:target, .content h6:target {{
                animation: none;
                background-color: var(--accent-light);
            }}
        }}
        .header-anchor {{
            position: absolute;
            left: -1.2rem;
            top: 0;
            bottom: 0;
            display: inline-flex;
            align-items: center;
            opacity: 0;
            text-decoration: none;
            font-weight: 400;
            color: var(--text-muted);
            transition: opacity 0.15s ease, color 0.15s ease;
        }}
        .header-anchor::before {{
            content: ""#"";
        }}
        .header-anchor:hover {{
            color: var(--accent);
        }}
        .content h2:hover .header-anchor, .content h3:hover .header-anchor,
        .content h4:hover .header-anchor, .content h5:hover .header-anchor,
        .content h6:hover .header-anchor, .header-anchor:focus {{
            opacity: 1;
        }}
        .content h2:not(.bark-feature-title) {{
            font-size: 1.4rem;
            font-weight: 500;
            letter-spacing: -0.02em;
            margin-top: 2.75rem;
            margin-bottom: 1rem;
            padding-bottom: 0.3rem;
            border-bottom: 1px solid var(--border);
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content p:not(.bark-hero-tagline):not(.bark-feature-details):not(.bark-hero-name):not(.custom-block-title) {{
            color: var(--text-color);
            margin-bottom: 1.25rem;
            text-decoration-color: var(--border);
            text-underline-offset: 2px;
        }}
        .content a {{
            color: var(--accent);
            text-decoration: underline;
            text-decoration-color: var(--border);
            text-underline-offset: 2px;
            transition: text-decoration-color 0.15s ease;
        }}
        .content a:hover {{
            text-decoration-color: var(--accent);
        }}
        .content a.bark-hero-action, .content a.bark-feature, .content a.pagination-link {{
            text-decoration: none;
        }}
        .content a.bark-feature {{
            color: inherit;
        }}
        .content ul, .content ol {{
            padding-left: 1.5rem;
            margin-bottom: 1.25rem;
        }}
        .content li {{
            margin-bottom: 0.4rem;
        }}
        .content li > ul, .content li > ol {{
            margin-top: 0.4rem;
            margin-bottom: 0;
        }}
        .content hr {{
            border: none;
            border-top: 1px solid var(--border);
            margin: 2.5rem 0;
        }}
        .content video {{
            display: block;
            width: 100%;
            height: auto;
            max-width: 100%;
            border: 1px solid var(--border);
            border-radius: 12px;
            background: #000;
            margin: 1.75rem 0;
        }}
        .content iframe {{
            display: block;
            width: 100%;
            max-width: 100%;
            aspect-ratio: 16 / 9;
            border: 1px solid var(--border);
            border-radius: 12px;
            margin: 1.75rem 0;
        }}
        .content h3 {{
            font-size: 1.15rem;
            font-weight: 500;
            letter-spacing: -0.01em;
            margin-top: 2rem;
            margin-bottom: 0.75rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h4 {{
            font-size: 1rem;
            font-weight: 500;
            margin-top: 1.5rem;
            margin-bottom: 0.5rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h5, .content h6 {{
            font-size: 0.9rem;
            font-weight: 600;
            margin-top: 1.25rem;
            margin-bottom: 0.5rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        pre {{
            background-color: var(--code-bg);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1.25rem;
            overflow-x: auto;
            font-family: var(--font-mono);
            font-size: 0.85rem;
            margin: 1.5rem 0;
        }}
        code {{
            font-family: var(--font-mono);
            font-size: 0.875em;
            color: var(--accent);
            background-color: color-mix(in oklab, var(--text-color) 7%, transparent);
            padding: 0.1875rem 0.375rem;
            border-radius: 4px;
            unicode-bidi: plaintext;
            -webkit-box-decoration-break: clone;
            box-decoration-break: clone;
        }}
        pre code {{
            padding: 0;
            background-color: transparent;
            border-radius: 0;
            color: inherit;
            font-size: inherit;
        }}
        dt {{
            font-weight: 700;
        }}
        dd {{
            margin-bottom: .5rem;
            margin-left: 0;
        }}
        .content h1 code, .content h2 code, .content h3 code,
        .content h4 code, .content h5 code, .content h6 code {{
            font-size: 0.9em;
            color: inherit;
            background-color: transparent;
            padding: 0;
        }}
        .content div[class^=""language-""] {{
            --line-start: 0;
            position: relative;
            margin: 1.5rem 0;
            background-color: var(--code-bg);
            border: 1px solid var(--border);
            border-radius: 8px;
        }}
        .content div[class^=""language-""] pre {{
            margin: 0;
            border: none;
            border-radius: 0;
            padding-top: 2rem;
        }}
        .content div[class^=""language-""] .lang {{
            position: absolute;
            top: 0.6rem;
            left: 1rem;
            right: auto;
            font-size: 0.7rem;
            color: var(--text-muted);
            font-family: var(--font-sans);
            text-transform: lowercase;
            user-select: none;
            z-index: 1;
        }}
        .content div[class^=""language-""] .code-title {{
            padding: 0.6rem 1rem;
            font-size: 0.8rem;
            font-family: var(--font-mono);
            color: var(--text-muted);
            border-bottom: 1px solid var(--border);
        }}
        .content div[class^=""language-""].has-title .lang {{
            display: none;
        }}
        .content div[class^=""language-""].has-title pre {{
            padding-top: 0.75rem;
        }}
        .shiki, .shiki span {{
            color: var(--shiki-light);
        }}
        .shiki {{
            background-color: var(--code-bg);
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .shiki, :root:not([data-theme=""light""]) .shiki span {{
                color: var(--shiki-dark);
            }}
        }}
        :root[data-theme=""dark""] .shiki, :root[data-theme=""dark""] .shiki span {{
            color: var(--shiki-dark);
        }}
        .content div[class^=""language-""] pre code {{
            display: block;
            width: fit-content;
            min-width: 100%;
        }}
        .content .line {{
            display: inline-block;
            min-width: 100%;
            min-height: 1.4em;
        }}
        .content .line.highlighted {{
            background-color: var(--accent-light);
            margin: 0 -1.25rem;
            padding: 0 1.25rem;
            width: calc(100% + 2.5rem);
            box-shadow: 2px 0 0 var(--accent) inset;
        }}
        .content .line.highlighted.error {{
            box-shadow: 2px 0 0 var(--alert-caution) inset;
        }}
        .content .line.highlighted.warning {{
            box-shadow: 2px 0 0 var(--alert-warning) inset;
        }}
        .content .line.diff {{
            margin: 0 -1.25rem;
            padding: 0 1.25rem;
            width: calc(100% + 2.5rem);
        }}
        .content .line.diff.add {{
            background-color: color-mix(in oklab, var(--alert-tip) 15%, transparent);
        }}
        .content .line.diff.remove {{
            background-color: color-mix(in oklab, var(--alert-caution) 15%, transparent);
            opacity: 0.7;
        }}
        .content div[class^=""language-""].has-focused-lines .line {{
            opacity: 0.5;
            filter: blur(0.06rem);
            transition: opacity 0.2s, filter 0.2s;
        }}
        .content div[class^=""language-""].has-focused-lines .line.has-focus {{
            opacity: 1;
            filter: none;
        }}
        .content .line-numbers-mode pre {{
            padding-left: 3.25rem;
        }}
        .content .line-numbers-mode code {{
            counter-reset: line-gutter var(--line-start, 0);
        }}
        .content .line-numbers-mode .line.highlighted,
        .content .line-numbers-mode .line.diff {{
            margin-left: -3.25rem;
            padding-left: 3.25rem;
            width: calc(100% + 4.5rem);
        }}
        .content .line-numbers-mode .line::before {{
            content: counter(line-gutter);
            counter-increment: line-gutter;
            display: inline-block;
            width: 2rem;
            margin-left: -3.25rem;
            margin-right: 1.25rem;
            text-align: right;
            color: var(--text-muted);
            user-select: none;
        }}
        .content .custom-block {{
            --block: var(--accent);
            margin: 1rem 0;
            padding: 1rem;
            border-radius: 8px;
            line-height: 1.5;
            font-size: 0.9rem;
            color: var(--text-color);
            background-color: color-mix(in oklab, var(--block) var(--callout-tint), var(--bg-color));
        }}
        .content .custom-block p:not(.custom-block-title) {{
            margin: 0;
        }}
        .content .custom-block.tip {{
            --block: var(--alert-tip);
        }}
        .content .custom-block.info {{
            --block: var(--alert-note);
        }}
        .content .custom-block.warning {{
            --block: var(--alert-warning);
        }}
        .content .custom-block.danger {{
            --block: var(--alert-caution);
        }}
        .content .custom-block.details {{
            --block: var(--text-muted);
        }}
        .content .custom-block-title {{
            font-weight: 700;
            margin: 0 0 0.5rem;
            color: var(--block);
        }}
        .content .custom-block a {{
            color: inherit;
            font-weight: 600;
            text-decoration: underline;
            text-decoration-color: currentColor;
            text-underline-offset: 2px;
        }}
        .content .custom-block a:hover {{
            opacity: 0.75;
        }}
        .content details.custom-block summary {{
            font-weight: 700;
            color: var(--block);
            cursor: pointer;
            margin: 0 0 0.5rem;
        }}
        .content details.custom-block:not([open]) summary {{
            margin-bottom: 0;
        }}
        .content .bark-code-group {{
            margin: 1.5rem 0;
        }}
        .content .bark-code-group .tabs {{
            display: flex;
            gap: 0.25rem;
            border-bottom: 1px solid var(--border);
        }}
        .content .bark-code-group .tabs input {{
            display: none;
        }}
        .content .bark-code-group .tabs label {{
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
            padding: 0.5rem 0.9rem;
            font-size: 0.85rem;
            color: var(--text-muted);
            cursor: pointer;
            border-bottom: 2px solid transparent;
            margin-bottom: -1px;
        }}
        .content .bark-code-group .tabs .tab-icon {{
            display: inline-block;
            width: 14px;
            height: 14px;
            flex-shrink: 0;
            background-color: currentColor;
            -webkit-mask: var(--icon) center / contain no-repeat;
            mask: var(--icon) center / contain no-repeat;
        }}
        .content .bark-code-group .blocks > div[class^=""language-""] {{
            display: none;
            margin-top: 0;
            border-top-left-radius: 0;
            border-top-right-radius: 0;
        }}
        .content .bark-code-group .blocks > div[class^=""language-""].active {{
            display: block;
        }}
        .content .bark-code-group .tabs label.active-tab {{
            color: var(--text-color);
            border-bottom-color: var(--accent);
        }}
        .table-wrapper {{
            overflow-x: auto;
            -webkit-overflow-scrolling: touch;
            margin: 1.5rem 0;
            border-radius: 6px;
        }}
        .task-list-item input[type=""checkbox""] {{
            width: 1em;
            height: 1em;
            margin: 0 0.4em 0 0;
            vertical-align: middle;
        }}
        .content table {{
            width: 100%;
            border-collapse: collapse;
            font-size: 0.875rem;
        }}
        .content th, .content td {{
            padding: 0.6rem 1rem;
            border: 1px solid var(--border);
            text-align: left;
            vertical-align: top;
        }}
        .content th {{
            background-color: var(--accent-light);
            font-weight: 600;
            color: var(--text-color);
        }}
        .content tr:nth-child(even) {{
            background-color: var(--code-bg);
        }}
        .code-block-wrapper {{
            position: relative;
        }}
        .code-block-buttons {{
            position: absolute;
            top: 0.5rem;
            right: 0.5rem;
            display: flex;
            gap: 0.25rem;
            opacity: 0;
            transition: opacity 0.15s ease;
        }}
        .code-block-wrapper:hover .code-block-buttons,
        .code-block-wrapper:focus-within .code-block-buttons {{
            opacity: 1;
        }}
        .code-block-buttons button {{
            background: var(--code-button-bg);
            border: 1px solid var(--code-button-border);
            border-radius: 6px;
            width: 32px;
            height: 32px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: var(--text-muted);
            cursor: pointer;
            flex-shrink: 0;
            transition: color 0.15s ease, border-color 0.15s ease;
        }}
        .code-block-buttons button svg {{
            display: block;
            pointer-events: none;
        }}
        .code-block-buttons button:hover {{
            color: var(--code-button-hover);
            border-color: var(--code-button-hover);
        }}
        .code-block-buttons button.copied {{
            color: var(--code-button-hover);
            border-color: var(--code-button-hover);
        }}
        .code-block-buttons button.failed {{
            opacity: 0.5;
        }}
        .code-block-buttons button[aria-pressed=""true""] {{
            color: var(--code-button-hover);
            border-color: var(--code-button-hover);
        }}
        .content div[class^=""language-""].show-result .line.diff.remove {{
            display: none;
        }}
        .content div[class^=""language-""].show-result .line.diff.add {{
            background-color: transparent;
        }}
        .code-block-buttons button:disabled {{
            cursor: default;
        }}
        .code-block-buttons .spin {{
            animation: code-btn-spin 0.7s linear infinite;
        }}
        @keyframes code-btn-spin {{
            to {{
                transform: rotate(360deg);
            }}
        }}
        @media (prefers-reduced-motion: reduce) {{
            .code-block-buttons .spin {{
                animation: none;
            }}
        }}
        .markdown-alert {{
            --block: var(--accent);
            padding: 0.75rem 1rem;
            margin: 1.5rem 0;
            border-radius: 8px;
            background-color: color-mix(in oklab, var(--block) var(--callout-tint), var(--bg-color));
        }}
        .markdown-alert-title {{
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-weight: 600;
            margin-bottom: 0.25rem;
            color: var(--block);
        }}
        .markdown-alert-title svg {{
            width: 18px;
            height: 18px;
            flex-shrink: 0;
            fill: currentColor;
        }}
        .markdown-alert-note {{
            --block: var(--alert-note);
        }}
        .markdown-alert-tip {{
            --block: var(--alert-tip);
        }}
        .markdown-alert-important {{
            --block: var(--alert-important);
        }}
        .markdown-alert-warning {{
            --block: var(--alert-warning);
        }}
        .markdown-alert-caution {{
            --block: var(--alert-caution);
        }}
        .markdown-alert > :last-child {{
            margin-bottom: 0;
        }}
        badge {{
            display: inline-flex;
            align-items: center;
            vertical-align: middle;
            margin: 0 0.3rem;
            padding: 0.15rem 0.55rem;
            border-radius: 6px;
            background-color: color-mix(in oklab, var(--alert-tip) 16%, var(--code-bg));
            color: var(--alert-tip);
            font-family: var(--font-sans);
            font-size: 0.7rem;
            font-weight: 600;
            letter-spacing: 0.02em;
            line-height: 1.5;
        }}
        badge[type=""info""] {{
            background-color: color-mix(in oklab, var(--alert-note) 16%, var(--code-bg));
            color: var(--alert-note);
        }}
        badge[type=""tip""] {{
            background-color: color-mix(in oklab, var(--alert-tip) 16%, var(--code-bg));
            color: var(--alert-tip);
        }}
        badge[type=""warning""] {{
            background-color: color-mix(in oklab, var(--alert-warning) 16%, var(--code-bg));
            color: var(--alert-warning);
        }}
        badge[type=""danger""] {{
            background-color: color-mix(in oklab, var(--alert-caution) 16%, var(--code-bg));
            color: var(--alert-caution);
        }}
        h1 badge, h2 badge, h3 badge, h4 badge {{
            font-size: 0.55em;
            margin-left: 0.5rem;
            vertical-align: middle;
        }}
        .pagination {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: 1.5rem;
            margin-top: 4rem;
            padding-top: 1.5rem;
            margin-bottom: 2.5rem;
            border-top: 1px solid var(--border);
        }}
        .pagination .pagination-link {{
            display: inline-flex;
            align-items: flex-start;
            gap: 0.5rem;
            min-width: 0;
            max-width: 50%;
            min-height: 24px;
            font-size: 0.95rem;
            font-weight: 600;
            line-height: 1.5;
            color: var(--text-color);
            text-decoration: none;
            border-radius: 4px;
            transition: color 0.15s ease;
        }}
        .pagination .pagination-link:hover {{
            color: var(--accent);
        }}
        .pagination-link svg {{
            width: 16px;
            height: 16px;
            flex-shrink: 0;
            margin-top: 0.1875rem;
            color: var(--text-muted);
            transition: color 0.15s ease, transform 0.15s ease;
        }}
        .pagination-link:hover svg {{
            color: var(--accent);
        }}
        .pagination-link.prev:hover svg {{
            transform: translateX(-2px);
        }}
        .pagination-link.next:hover svg {{
            transform: translateX(2px);
        }}
        .pagination-link.next {{
            text-align: right;
            margin-left: auto;
        }}
        .toc-column {{
            display: flex;
            align-items: flex-start;
            gap: 0.5rem;
            flex: 0 0 auto;
            margin-left: auto;
            position: sticky;
            top: var(--topbar-height);
            align-self: flex-start;
            box-sizing: border-box;
            padding-top: 3rem;
        }}
        .sidebar-right {{
            width: 16.5rem;
            max-height: calc(100vh - var(--topbar-height) - 3rem);
            box-sizing: border-box;
            overflow-y: auto;
            overscroll-behavior: contain;
            padding: 0.25rem 0.25rem 2rem 0;
        }}
        .sidebar-right[hidden] {{
            display: none;
        }}
        .toc-title {{
            display: flex;
            align-items: center;
            gap: 0.5rem;
            margin: 0 0 0.75rem;
            font-size: 0.875rem;
            line-height: 1.4;
            color: var(--text-color);
            font-weight: 600;
        }}
        .toc-title svg {{
            width: 16px;
            height: 16px;
            flex-shrink: 0;
            color: var(--text-muted);
        }}
        .toc-collapse-toggle {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 0.25rem;
            min-width: 28px;
            height: 28px;
            padding: 0 0.4rem;
            box-sizing: border-box;
            border-radius: 999px;
            border: 1px solid var(--border);
            background: var(--bg-color);
            color: var(--text-muted);
            cursor: pointer;
            transition: color 0.15s ease, border-color 0.15s ease;
        }}
        .toc-collapse-toggle:hover {{
            color: var(--text-color);
            border-color: var(--text-muted);
        }}
        .toc-collapse-toggle:focus-visible {{
            outline: 2px solid var(--accent);
            outline-offset: 2px;
        }}
        .toc-collapse-toggle svg {{
            width: 14px;
            height: 14px;
            flex-shrink: 0;
        }}
        .toc-collapse-toggle .toc-chevron {{
            transition: transform 0.15s ease;
        }}
        .toc-collapse-toggle .toc-glyph {{
            display: none;
        }}
        .toc-collapsed .toc-collapse-toggle .toc-chevron {{
            transform: rotate(180deg);
        }}
        .toc-collapsed .toc-collapse-toggle .toc-glyph {{
            display: block;
        }}
        .toc-list-wrapper {{
            position: relative;
        }}
        .toc-list-wrapper::before {{
            content: """";
            position: absolute;
            left: 0;
            top: 0;
            bottom: 0;
            width: 2px;
            border-radius: 2px;
            background-color: var(--accent-light);
        }}
        .toc-indicator {{
            position: absolute;
            left: 0;
            top: 0;
            width: 2px;
            height: 1px;
            background-color: var(--accent);
            opacity: 0;
            transform-origin: top;
            transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s ease;
            will-change: transform;
        }}
        .toc-indicator.visible {{
            opacity: 1;
        }}
        .toc-list {{
            list-style: none;
            font-size: 0.875rem;
            padding-left: 0.9rem;
        }}
        .toc-sublist {{
            list-style: none;
            padding-left: 0.9rem;
        }}
        .toc-item {{
            margin-bottom: 0.1rem;
        }}
        .toc-list > .toc-item > a {{
            color: var(--text-color);
        }}
        .toc-list > .toc-item > .toc-sublist > .toc-item > a {{
            font-weight: 400;
        }}
        .toc-list > .toc-item > .toc-sublist > .toc-item > .toc-sublist > .toc-item > a {{
            font-weight: 400;
            font-size: 0.8rem;
        }}
        .toc-item a {{
            display: block;
            color: var(--text-muted);
            line-height: 1.5;
            text-decoration: none;
            padding: 0.3rem 0.8rem;
            margin-left: -0.8rem;
            transition: color 0.15s ease;
            overflow-wrap: anywhere;
        }}
        .toc-item a:hover {{
            color: var(--text-color);
        }}
        .toc-item.active > a {{
            color: var(--accent);
            font-weight: 600;
        }}
        .social-links {{
            display: flex;
            align-items: center;
            gap: 0.25rem;
        }}
        .social-icon-text {{
            font-size: 0.9rem;
        }}
        .sidebar-social-links {{
            display: none;
        }}
        .site-footer {{
            padding: 0 3rem 1.25rem 4rem;
        }}
        .bark-home-layout .site-footer {{
            width: 100%;
            max-width: 960px;
            margin: 0 auto;
            padding: 0 2rem 1.25rem;
        }}
        @media (min-width: 1500px) {{
            .bark-home-layout .site-footer {{
                max-width: 1180px;
            }}
        }}
        .footer-bar {{
            border-top: 1px solid var(--border);
            padding-top: 1.25rem;
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: center;
            gap: 0.5rem 1.5rem;
            text-align: center;
        }}
        .footer-bar--split {{
            justify-content: space-between;
            text-align: left;
        }}
        .footer-nav {{
            display: flex;
            flex-wrap: wrap;
            gap: 0.25rem 1.25rem;
        }}
        .footer-nav a {{
            display: inline-flex;
            align-items: center;
            gap: 0.25rem;
            font-size: 0.8rem;
            color: var(--text-muted);
            text-decoration: none;
        }}
        .footer-nav a:hover {{
            color: var(--text-color);
        }}
        .content-footer {{
            font-size: 0.8rem;
            color: var(--text-muted);
        }}
        .content-footer a {{
            color: var(--accent);
            text-decoration: none;
        }}
        .content-footer a:hover {{
            text-decoration: underline;
        }}
        .menu-toggle {{
            display: none;
        }}
        .sidebar-overlay {{
            display: none;
        }}
        .toc-inline {{
            display: none;
        }}
        @media (hover: none) and (pointer: coarse) {{
            .pagination .pagination-link {{
                align-items: center;
                min-height: 44px;
            }}
            .pagination-link svg {{
                margin-top: 0;
            }}
            .icon-btn, .repo-widget {{
                width: 44px;
                height: 44px;
            }}
            .nav-item a, .toc-item a {{
                min-height: 44px;
                display: flex;
                align-items: center;
            }}
            .code-block-buttons {{
                opacity: 1;
            }}
        }}
        @media (max-width: 1279px) {{
            .toc-column {{
                display: none;
            }}
            .main-column,
            .toc-collapsed .main-column {{
                max-width: 960px;
            }}
        }}
        @media (max-width: 1024px) {{
            .layout {{
                grid-template-columns: 15rem minmax(0, 1fr);
                padding: 0 1.5rem;
            }}
            .main-container {{
                padding: 2rem 0 2rem 1.5rem;
            }}
            .site-footer {{
                padding: 0 0 1.25rem 1.5rem;
            }}
        }}
        @media (min-width: 769px) and (max-width: 1279px) {{
            .toc-inline {{
                display: block;
                margin-bottom: 2rem;
                border: 1px solid var(--border);
                border-radius: 8px;
                padding: 0.5rem 1rem;
            }}
            .toc-inline summary {{
                cursor: pointer;
                font-size: 0.8rem;
                font-weight: 600;
                letter-spacing: 0.02em;
                color: var(--text-muted);
                padding: 0.5rem 0;
                list-style: none;
                display: flex;
                align-items: center;
                justify-content: space-between;
            }}
            .toc-inline summary::-webkit-details-marker {{
                display: none;
            }}
            .toc-inline summary::after {{
                content: """";
                display: inline-block;
                width: 6px;
                height: 6px;
                flex-shrink: 0;
                border-right: 2px solid var(--text-muted);
                border-bottom: 2px solid var(--text-muted);
                transform: rotate(-45deg);
                transition: transform 0.2s ease;
            }}
            .toc-inline[open] summary::after {{
                transform: rotate(45deg);
            }}
            .toc-inline .toc-list {{
                padding-bottom: 0.5rem;
            }}
            .toc-inline .toc-item a {{
                padding-left: 0.5rem;
                border-left: none;
            }}
        }}
        @media (max-width: 768px) {{
            .layout {{
                grid-template-columns: minmax(0, 1fr);
                padding: 0;
            }}
            .bark-home-layout .sidebar-left {{
                display: block;
            }}
            .main-container {{
                padding: 2rem 1.5rem;
            }}
            pre {{
                padding: 1rem;
                font-size: 0.8125rem;
            }}
            .content .line.highlighted,
            .content .line.diff {{
                margin: 0 -1rem;
                padding: 0 1rem;
                width: calc(100% + 2rem);
            }}
            .content .line-numbers-mode .line.highlighted,
            .content .line-numbers-mode .line.diff {{
                margin-left: -3.25rem;
                padding-left: 3.25rem;
                width: calc(100% + 4.25rem);
            }}
            .site-footer,
            .bark-home-layout .site-footer {{
                padding: 0 1.5rem 2rem;
            }}
            .bark-hero {{
                padding: 2.5rem 1.5rem 3rem;
            }}
            .bark-hero-name,
            .bark-hero-text {{
                font-size: 1.9rem;
            }}
            .bark-hero-tagline {{
                font-size: 1rem;
                margin-bottom: 2rem;
            }}
            .menu-toggle {{
                display: inline-flex;
            }}
            .top-nav {{
                display: none;
            }}
            .mobile-top-nav {{
                display: block;
                margin-bottom: 1.25rem;
                padding-bottom: 1.25rem;
                border-bottom: 1px solid var(--border);
            }}
            .mobile-top-nav:last-child,
            .mobile-top-nav:has(+ .sidebar-social-links) {{
                margin-bottom: 0;
                padding-bottom: 0;
                border-bottom: none;
            }}
            .mobile-top-nav-link,
            .mobile-top-nav-group summary {{
                display: flex;
                align-items: center;
                gap: 0.4rem;
                min-height: 44px;
                padding: 0 1rem;
                border-radius: 6px;
                font-size: 0.875rem;
                font-weight: 600;
                line-height: 1.4;
                color: var(--text-color);
                text-decoration: none;
                transition: background-color 0.15s ease, color 0.15s ease;
            }}
            .mobile-top-nav-link:hover,
            .mobile-top-nav-group summary:hover {{
                background-color: var(--nav-hover-bg);
            }}
            .mobile-top-nav-link.active {{
                color: var(--accent);
                background-color: var(--nav-active-bg);
            }}
            .mobile-top-nav-group summary {{
                cursor: pointer;
                list-style: none;
            }}
            .mobile-top-nav-group summary::-webkit-details-marker {{
                display: none;
            }}
            .mobile-top-nav-group summary > span:first-child {{
                flex: 1;
            }}
            .mobile-top-nav-group[open] > summary .caret-icon {{
                transform: rotate(90deg);
            }}
            .mobile-top-nav-group .mobile-top-nav-link {{
                font-weight: 400;
                color: var(--text-muted);
            }}
            .mobile-top-nav-group .mobile-top-nav-link.active {{
                color: var(--accent);
            }}
            .sidebar-left {{
                position: fixed;
                top: var(--drawer-top);
                left: 0;
                height: calc(100dvh - var(--drawer-top));
                width: 280px;
                max-width: 85vw;
                z-index: 1003;
                padding: 1.5rem 1rem;
                background-color: var(--sidebar-bg);
                transform: translateX(-100%);
                visibility: hidden;
                transition: transform 0.2s ease, visibility 0s linear 0.2s;
            }}
            .sidebar-left.open {{
                transform: translateX(0);
                visibility: visible;
                box-shadow: var(--shadow-lg);
                transition: transform 0.2s ease;
            }}
            .sidebar-overlay.open {{
                display: block;
                position: fixed;
                top: var(--drawer-top);
                left: 0;
                right: 0;
                bottom: 0;
                background: var(--overlay-bg);
                z-index: 1001;
            }}
            .nav-item a, .toc-item a {{
                min-height: 44px;
                display: flex;
                align-items: center;
            }}
            .search-result-title {{
                font-size: 0.95rem;
            }}
            .search-result-excerpt {{
                font-size: 0.8rem;
            }}
            .topbar-right .social-links {{
                display: none;
            }}
            .sidebar-social-links {{
                display: flex;
                flex-wrap: wrap;
                gap: 0.25rem;
                padding: 1.25rem 0 0.25rem;
                border-top: 1px solid var(--border);
            }}
        }}
        @media (max-width: 380px) {{
            .topbar-inner {{
                padding: 0 0.75rem;
            }}
            .topbar-left {{
                gap: 0.5rem;
            }}
            .topbar-right {{
                gap: 0;
            }}
            .brand a {{
                white-space: nowrap;
            }}
        }}
        @media (prefers-reduced-motion: reduce) {{
            *, *::before, *::after {{
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }}
        }}
{themeComponentCss}
"));

    // CSS has no line comments or ASI to break, so collapsing all whitespace is safe here unlike in MinifyJs
    private static string MinifyCss(string css) =>
        System.Text.RegularExpressions.Regex.Replace(
            System.Text.RegularExpressions.Regex.Replace(css, @"/\*[\s\S]*?\*/", ""),
            @"[ \t]*[\r\n]+[ \t]*|[ \t]{2,}", " ").Trim();
}
