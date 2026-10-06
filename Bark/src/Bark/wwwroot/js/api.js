// API reference pages: sample panels, response switching and the playground.
// Playground requests go from the browser to the API; Bark never proxies them.
(function () {
    'use strict';

    var root = document.querySelector('.bark-api');
    if (!root) return;
    root.classList.add('api-ready');

    var scriptSrc = (document.currentScript && document.currentScript.src) || (document.querySelector('script[src*="/js/api.js"]') || {}).src || '';
    var iconBase = scriptSrc ? new URL('../icons/', scriptSrc).href : '/icons/';

    function el(tag, attrs, children) {
        var node = document.createElement(tag);
        if (attrs) Object.keys(attrs).forEach(function (key) {
            var value = attrs[key];
            if (value === null || value === undefined || value === false) return;
            if (key === 'text') node.textContent = value;
            else if (key === 'class') node.className = value;
            else if (key.slice(0, 2) === 'on') node.addEventListener(key.slice(2), value);
            else node.setAttribute(key, value === true ? '' : value);
        });
        (children || []).forEach(function (child) { if (child) node.appendChild(child); });
        return node;
    }

    // Token colors of Bark's github-light/github-dark highlighter, so browser-rendered JSON matches server-rendered JSON.
    var BASE_STYLE = '--shiki-light:#24292e;--shiki-dark:#e1e4e8;';
    var TOKEN_STYLE = {
        key: '--shiki-light:#005CC5;--shiki-dark:#79B8FF',
        string: '--shiki-light:#032F62;--shiki-dark:#9ECBFF',
        literal: '--shiki-light:#005CC5;--shiki-dark:#79B8FF',
    };
    var JSON_TOKEN = /("(?:\\.|[^"\\])*")(\s*:)?|\b(?:true|false|null)\b|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/g;

    function highlightLine(line, json) {
        var span = el('span', { class: 'line' });
        if (!json) {
            span.textContent = line;
            return span;
        }
        var last = 0;
        line.replace(JSON_TOKEN, function (match, str, colon, offset) {
            if (offset > last) span.appendChild(document.createTextNode(line.slice(last, offset)));
            var kind = str ? (colon ? 'key' : 'string') : 'literal';
            span.appendChild(el('span', { style: TOKEN_STYLE[kind], text: str || match }));
            if (colon) span.appendChild(document.createTextNode(colon));
            last = offset + match.length;
            return match;
        });
        if (last < line.length) span.appendChild(document.createTextNode(line.slice(last)));
        return span;
    }

    function fillCode(code, text, json) {
        code.textContent = '';
        text.split('\n').forEach(function (line, i) {
            if (i) code.appendChild(document.createTextNode('\n'));
            code.appendChild(highlightLine(line, json));
        });
    }

    function shikiPre(attrs) {
        var pre = el('pre', attrs);
        pre.classList.add('shiki', 'shiki-themes', 'github-light', 'github-dark');
        pre.setAttribute('style', BASE_STYLE);
        return pre;
    }

    // A Bark code block; copy and download come from Bark's own code block script.
    function codeBlock(text, json) {
        var code = el('code');
        fillCode(code, text, json);
        var pre = shikiPre({ tabindex: '0', dir: 'ltr' });
        pre.appendChild(code);
        var block = el('div', { class: 'language-' + (json ? 'json' : 'txt') }, [pre]);
        if (window.barkEnhanceCodeBlock) window.barkEnhanceCodeBlock(pre);
        return block;
    }

    // Language dropdowns: one choice shared by every panel on the page. Nothing is persisted.
    function setIcon(select) {
        var option = select.options[select.selectedIndex];
        var label = select.closest('.api-lang');
        var icon = label.querySelector('.api-lang-icon');
        if (!icon) {
            icon = el('span', { class: 'api-lang-icon', 'aria-hidden': 'true' });
            label.insertBefore(icon, select);
        }
        var slug = option && option.getAttribute('data-icon');
        icon.style.setProperty('--icon', slug ? 'url("' + iconBase + slug + '.svg")' : 'none');
        icon.hidden = !slug;
    }

    function showSample(panel, name) {
        var samples = panel.querySelectorAll('.api-sample');
        var found = Array.prototype.some.call(samples, function (s) { return s.getAttribute('data-sample') === name; });
        if (found) samples.forEach(function (s) { s.hidden = s.getAttribute('data-sample') !== name; });
        return found;
    }

    function applyLang(name) {
        document.querySelectorAll('[data-api-samples]').forEach(function (panel) {
            var select = panel.querySelector('[data-api-lang]');
            if (!select || !showSample(panel, name)) return;
            select.value = name;
            setIcon(select);
        });
    }

    document.querySelectorAll('[data-api-lang]').forEach(function (select) {
        setIcon(select);
        select.addEventListener('change', function () { applyLang(select.value); });
    });

    // Response status: tabs in the example panel and the dropdown in the Response section select together.
    var statusSelect = root.querySelector('[data-api-status]');
    var contentTypeLabel = root.querySelector('[data-api-content-type]');
    var statusPanel = root.querySelector('[data-api-responses]');
    // Captured before Bark's code block script runs, so the playground copy gets live buttons of its own.
    var statusPanelSource = statusPanel ? statusPanel.cloneNode(true) : null;

    function wireStatusTabs(panel, onSelect) {
        var tabs = Array.prototype.slice.call(panel.querySelectorAll('.api-status-tab'));
        function select(status, focus) {
            showSample(panel, status);
            tabs.forEach(function (tab) {
                var active = tab.getAttribute('data-status') === status;
                tab.setAttribute('aria-selected', String(active));
                tab.tabIndex = active ? 0 : -1;
                if (active && focus) tab.focus();
            });
            if (onSelect) onSelect(status);
        }
        tabs.forEach(function (tab, index) {
            tab.addEventListener('click', function () { select(tab.getAttribute('data-status')); });
            tab.addEventListener('keydown', function (e) {
                var next = e.key === 'ArrowRight' ? index + 1 : e.key === 'ArrowLeft' ? index - 1 : null;
                if (next === null) return;
                e.preventDefault();
                select(tabs[(next + tabs.length) % tabs.length].getAttribute('data-status'), true);
            });
        });
        return select;
    }

    function showResponse(status) {
        root.querySelectorAll('.api-response').forEach(function (r) {
            if (r.getAttribute('data-status') === status) {
                r.removeAttribute('data-api-inactive');
                if (contentTypeLabel) contentTypeLabel.textContent = r.getAttribute('data-content-type') || '';
            } else {
                r.setAttribute('data-api-inactive', '');
            }
        });
        if (statusSelect) statusSelect.value = status;
    }

    var selectStatusTab = statusPanel ? wireStatusTabs(statusPanel, showResponse) : null;
    if (statusSelect) statusSelect.addEventListener('change', function () {
        showResponse(statusSelect.value);
        if (selectStatusTab) selectStatusTab(statusSelect.value);
    });

    // Playground.
    var dataEl = root.querySelector('.bark-api-data');
    var tryButtons = root.querySelectorAll('[data-api-try]');
    if (!dataEl || !tryButtons.length) return;

    var data;
    try { data = JSON.parse(dataEl.textContent); } catch (_) { return; }
    var L = data.labels;
    var dialog = null;
    var controller = null;
    // Credentials live in memory only: gone on reload or navigation, never written to browser storage.
    var state = { values: {}, auth: {} };

    var uid = 0;
    function row(name, type, flags, description, control, note) {
        var id = 'api-pg-' + (++uid);
        (control.querySelector('input, select, textarea') || control).id = id;
        return el('div', { class: 'api-pg-row' }, [
            el('div', { class: 'api-pg-label' }, [
                el('label', { for: id, class: 'api-pg-name', text: name }),
                type ? el('span', { class: 'api-field-type', text: type }) : null,
            ].concat(flags.filter(Boolean).map(function (f) {
                return el('span', { class: 'api-field-flag' + (f === L.apiRequired ? ' api-field-required' : ''), text: f });
            })).concat([
                description ? el('p', { class: 'api-pg-desc', text: description }) : null,
                note ? el('p', { class: 'api-pg-desc', text: note }) : null,
            ])),
            control,
        ]);
    }

    function input(value, onInput, options) {
        options = options || {};
        if ((options.enumValues && options.enumValues.length) || /^boolean/.test(options.type || '')) {
            var values = options.enumValues && options.enumValues.length ? options.enumValues : ['true', 'false'];
            var select = el('select', { class: 'api-pg-input', onchange: function () { onInput(select.value); } },
                [el('option', { value: '', text: '' })].concat(values.map(function (v) {
                    return el('option', { value: v, text: v, selected: v === value });
                })));
            return select;
        }
        var node = el('input', {
            class: 'api-pg-input',
            type: options.secret ? 'password' : 'text',
            value: value || '',
            placeholder: options.placeholder || '',
            autocomplete: options.secret ? 'new-password' : 'off',
            spellcheck: 'false',
            // Tokens typed here are not site logins; keep password managers from offering to fill or save them.
            'data-bwignore': 'true',
            'data-1p-ignore': 'true',
            'data-lpignore': 'true',
            'data-form-type': 'other',
            inputmode: /^(integer|number)/.test(options.type || '') ? 'decimal' : null,
            oninput: function () { onInput(node.value); },
        });
        return node;
    }

    // Textarea over a highlighted copy of its own text; the textarea keeps native editing, selection and undo.
    function editor(value, onInput, rows, json, label) {
        var code = el('code');
        var view = shikiPre({ class: 'api-editor-view', 'aria-hidden': 'true' });
        view.appendChild(code);
        var area = el('textarea', { class: 'api-editor-input', rows: String(rows), spellcheck: 'false', 'aria-label': label });
        area.value = value || '';
        function render() { fillCode(code, area.value + '\n', json); }
        area.addEventListener('input', function () { render(); onInput(area.value); });
        area.addEventListener('scroll', function () { view.scrollTop = area.scrollTop; view.scrollLeft = area.scrollLeft; });
        area.addEventListener('keydown', function (e) {
            if (e.key !== 'Tab' || e.shiftKey) return;
            e.preventDefault();
            area.setRangeText('  ', area.selectionStart, area.selectionEnd, 'end');
            render();
            onInput(area.value);
        });
        render();
        return el('div', { class: 'api-editor' }, [view, area]);
    }

    var CHEVRON = '<svg class="api-chevron" viewBox="0 0 24 24" aria-hidden="true"><path d="M9 6l6 6-6 6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>';

    function card(title, children) {
        children = children.filter(Boolean);
        if (!children.length) return null;
        var summary = el('summary', null, [el('span', { text: title })]);
        summary.insertAdjacentHTML('afterbegin', CHEVRON);
        return el('details', { class: 'api-pg-card', open: true }, [summary, el('div', { class: 'api-pg-card-body' }, children)]);
    }

    function paramKey(p) { return p.in + ':' + p.name; }

    function authRows() {
        return data.auth.map(function (auth) {
            var key = auth.kind + ':' + auth.name;
            if (auth.kind === 'basic') {
                var basic = state.auth[key] || {};
                return row(auth.name, 'string', ['header', L.apiRequired], auth.description, el('div', { class: 'api-pg-pair' }, [
                    input(basic.user, function (v) { basic.user = v; state.auth[key] = basic; }, { placeholder: 'username' }),
                    input(basic.pass, function (v) { basic.pass = v; state.auth[key] = basic; }, { secret: true, placeholder: 'password' }),
                ]));
            }
            return row(auth.name, 'string', [auth.in, L.apiRequired], auth.description,
                input(state.auth[key], function (v) { state.auth[key] = v; },
                    { secret: true, placeholder: auth.kind === 'bearer' ? 'token' : auth.name }),
                auth.in === 'cookie' ? L.apiCookieBlocked : null);
        });
    }

    function paramRows(location) {
        return data.params.filter(function (p) { return p.in === location; }).map(function (p) {
            var key = paramKey(p);
            if (!(key in state.values)) state.values[key] = p.value || '';
            return row(p.name, p.type, [p.required ? L.apiRequired : null], p.description,
                input(state.values[key], function (v) { state.values[key] = v; }, { type: p.type, enumValues: p.enum }),
                location === 'cookie' ? L.apiCookieBlocked : null);
        });
    }

    function isForm(contentType) {
        return /^(multipart\/|application\/x-www-form-urlencoded)/i.test(contentType || '');
    }

    function bodyCards() {
        if (data.graphql) {
            if (!('gql:query' in state.values)) state.values['gql:query'] = data.graphql.query;
            if (!('gql:variables' in state.values)) state.values['gql:variables'] = data.graphql.variables;
            return [
                card(L.apiQuery, [editor(state.values['gql:query'], function (v) { state.values['gql:query'] = v; }, 10, false, L.apiQuery)]),
                card(L.apiVariables, [editor(state.values['gql:variables'], function (v) { state.values['gql:variables'] = v; }, 6, true, L.apiVariables)]),
            ];
        }
        if (!data.body) return [];
        if (isForm(data.body.contentType)) {
            return [card(L.apiBody, data.body.fields.map(function (f) {
                var key = 'form:' + f.name;
                if (f.file) {
                    var file = el('input', { type: 'file', class: 'api-pg-input', onchange: function () { state.values[key] = file.files[0] || null; } });
                    return row(f.name, f.type, [f.required ? L.apiRequired : null], f.description, file);
                }
                if (!(key in state.values)) state.values[key] = f.value || '';
                return row(f.name, f.type, [f.required ? L.apiRequired : null], f.description,
                    input(state.values[key], function (v) { state.values[key] = v; }, { type: f.type }));
            }))];
        }
        if (!('body' in state.values)) state.values.body = data.body.example === 'null' ? '' : data.body.example;
        var json = /json/i.test(data.body.contentType);
        return [card(L.apiBody + ' · ' + data.body.contentType, [
            editor(state.values.body, function (v) { state.values.body = v; }, 14, json, L.apiBody),
        ])];
    }

    function odataLiteral(p, value) {
        return data.kind === 'odata' && p && /^string$/.test(p.type) ? "'" + value.replace(/'/g, "''") + "'" : value;
    }

    function buildRequest(server) {
        var path = data.path.replace(/\{([^}]+)\}/g, function (match, name) {
            var p = data.params.filter(function (x) { return x.in === 'path' && x.name === name; })[0];
            var value = state.values['path:' + name] || '';
            return value ? encodeURIComponent(odataLiteral(p, value)).replace(/%27/g, "'") : match;
        });
        var url = server.replace(/\/+$/, '') + path;
        var query = new URLSearchParams();
        var raw = [];
        var headers = new Headers();

        data.params.forEach(function (p) {
            var value = state.values[paramKey(p)];
            if (value === undefined || value === '') return;
            if (p.in === 'query') query.append(p.name, value);
            else if (p.in === 'querystring') raw.push(value.replace(/^\?/, ''));
            else if (p.in === 'header') headers.set(p.name, value);
        });

        data.auth.forEach(function (auth) {
            var value = state.auth[auth.kind + ':' + auth.name];
            if (!value) return;
            if (auth.kind === 'basic') {
                if (value.user || value.pass) headers.set('Authorization', 'Basic ' + btoa(unescape(encodeURIComponent((value.user || '') + ':' + (value.pass || '')))));
            } else if (auth.kind === 'bearer') {
                headers.set(auth.name, /^bearer /i.test(value) ? value : 'Bearer ' + value);
            } else if (auth.in === 'query') {
                query.append(auth.name, value);
            } else if (auth.in === 'header') {
                headers.set(auth.name, value);
            }
        });

        var qs = query.toString();
        if (qs) raw.unshift(qs);
        if (raw.length) url += (url.indexOf('?') < 0 ? '?' : '&') + raw.join('&');

        var body;
        if (data.graphql) {
            var variables = (state.values['gql:variables'] || '').trim();
            body = JSON.stringify({ query: state.values['gql:query'], variables: variables ? JSON.parse(variables) : {} });
            headers.set('Content-Type', 'application/json');
        } else if (data.body) {
            var type = data.body.contentType;
            if (isForm(type)) {
                var multipart = /^multipart\//i.test(type);
                body = multipart ? new FormData() : new URLSearchParams();
                data.body.fields.forEach(function (f) {
                    var value = state.values['form:' + f.name];
                    if (value !== undefined && value !== null && value !== '') body.append(f.name, value);
                });
                if (!multipart) headers.set('Content-Type', type);
            } else {
                var text = state.values.body || '';
                if (text.trim()) {
                    if (/json/i.test(type)) JSON.parse(text);
                    headers.set('Content-Type', type);
                    body = text;
                }
            }
        }
        return { url: url, init: { method: data.method, headers: headers, body: body, credentials: 'omit', mode: 'cors' } };
    }

    function format(template, value) { return template.replace('{0}', value); }

    function showResult(result, payload) {
        result.textContent = '';
        if (payload.error) {
            result.appendChild(el('p', { class: 'api-pg-error', role: 'alert', text: payload.error }));
            return;
        }
        var status = String(payload.status);
        var json = false;
        var text = payload.body;
        try { text = JSON.stringify(JSON.parse(payload.body), null, 2); json = true; } catch (_) {}
        result.appendChild(el('div', { class: 'api-panel' }, [
            el('div', { class: 'api-panel-head' }, [
                el('span', { class: 'api-panel-title', text: L.apiResponse }),
                el('div', { class: 'api-panel-tools' }, [
                    el('span', { class: 'api-pg-metric', text: format(L.apiTime, payload.ms) }),
                    el('span', { class: 'api-pg-metric', text: format(L.apiSize, payload.size) }),
                    el('span', { class: 'api-status', 'data-status': status.charAt(0), text: status + (payload.statusText ? ' ' + payload.statusText : '') }),
                ]),
            ]),
            el('div', { class: 'api-panel-body' }, [codeBlock(text || '', json)]),
        ]));
        if (payload.headers.length) {
            result.appendChild(el('details', { class: 'api-pg-headers' }, [
                el('summary', { text: L.apiResponseHeaders }),
                el('dl', null, payload.headers.reduce(function (nodes, h) {
                    nodes.push(el('dt', { text: h[0] }), el('dd', { text: h[1] }));
                    return nodes;
                }, [])),
            ]));
        }
    }

    function exampleResponses() {
        if (!statusPanelSource) return el('p', { class: 'api-pg-empty', text: L.apiResponseEmpty });
        var panel = statusPanelSource.cloneNode(true);
        panel.querySelectorAll('pre').forEach(function (pre) { if (window.barkEnhanceCodeBlock) window.barkEnhanceCodeBlock(pre); });
        wireStatusTabs(panel);
        return panel;
    }

    function send(server, sendButton, result) {
        var request;
        try {
            request = buildRequest(server);
        } catch (e) {
            showResult(result, { error: L.apiInvalidJson + ' ' + e.message });
            return;
        }
        if (controller) controller.abort();
        controller = new AbortController();
        request.init.signal = controller.signal;
        sendButton.disabled = true;
        sendButton.querySelector('span').textContent = L.apiSending;
        result.setAttribute('aria-busy', 'true');
        var started = performance.now();

        fetch(request.url, request.init)
            .then(function (response) {
                return response.text().then(function (text) {
                    var headers = [];
                    response.headers.forEach(function (value, name) { headers.push([name, value]); });
                    showResult(result, {
                        status: response.status,
                        statusText: response.statusText,
                        ms: Math.round(performance.now() - started),
                        size: new Blob([text]).size,
                        body: text,
                        headers: headers,
                    });
                });
            })
            .catch(function (error) {
                if (error.name !== 'AbortError') showResult(result, { error: L.apiRequestFailed });
            })
            .then(function () {
                sendButton.disabled = false;
                sendButton.querySelector('span').textContent = L.apiSend;
                result.removeAttribute('aria-busy');
            });
    }

    function buildDialog() {
        var titleId = 'api-pg-title';
        var servers = data.servers.length ? data.servers : [''];
        var serverControl = servers.length > 1
            ? el('select', { class: 'api-pg-server', 'aria-label': L.apiServer }, servers.map(function (s) { return el('option', { value: s, text: s }); }))
            : el('span', { class: 'api-pg-server', text: servers[0] });
        function currentServer() { return serverControl.tagName === 'SELECT' ? serverControl.value : serverControl.textContent; }

        var sendButton = el('button', { type: 'submit', class: 'api-pg-send' }, [el('span', { text: L.apiSend })]);
        sendButton.insertAdjacentHTML('beforeend', '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 5v14l11-7z" fill="currentColor"/></svg>');
        var closeButton = el('button', { type: 'button', class: 'api-pg-close icon-btn', 'aria-label': L.apiClose, onclick: function () { dialog.close(); } });
        closeButton.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M18 6L6 18M6 6l12 12"/></svg>';

        var result = el('div', { class: 'api-pg-result', 'aria-live': 'polite' }, [exampleResponses()]);

        var inputs = el('div', { class: 'api-pg-inputs' }, [
            el('div', { class: 'api-pg-intro' }, [
                el('h2', { id: titleId, class: 'api-pg-title', text: data.title }),
                data.description ? el('p', { class: 'api-pg-desc', text: data.description }) : null,
            ]),
            card(L.apiAuthorizations, authRows().concat(data.auth.length ? [el('p', { class: 'api-pg-note', text: L.apiCredentialsNote })] : [])),
            card(L.apiPathParameters, paramRows('path')),
            card(L.apiQueryParameters, paramRows('query')),
            card(L.apiQueryString, paramRows('querystring')),
            card(L.apiHeaders, paramRows('header')),
            card(L.apiCookies, paramRows('cookie')),
        ].concat(bodyCards()));

        var form = el('form', { class: 'api-pg', novalidate: true, autocomplete: 'off', 'data-bwignore': 'true', 'data-1p-ignore': 'true', 'data-lpignore': 'true', 'data-form-type': 'other', onsubmit: function (e) {
            e.preventDefault();
            send(currentServer(), sendButton, result);
        } }, [
            el('div', { class: 'api-pg-head' }, [
                el('div', { class: 'api-pg-url' }, [
                    el('span', { class: 'api-method', 'data-method': data.method.toLowerCase(), text: data.method }),
                    serverControl,
                    el('span', { class: 'api-pg-path', text: data.path }),
                ]),
                sendButton,
                closeButton,
            ]),
            el('div', { class: 'api-pg-body' }, [inputs, result]),
        ]);

        dialog = el('dialog', { class: 'api-playground', 'aria-labelledby': titleId }, [form]);
        dialog.addEventListener('click', function (e) { if (e.target === dialog) dialog.close(); });
        dialog.addEventListener('close', function () { if (controller) controller.abort(); });
        document.body.appendChild(dialog);
    }

    tryButtons.forEach(function (button) {
        button.setAttribute('aria-haspopup', 'dialog');
        button.addEventListener('click', function () {
            if (!dialog) buildDialog();
            dialog.showModal();
        });
    });
})();
