(function () {
'use strict';
const $ = (s, r) => (r || document).querySelector(s);
const $$ = (s, r) => [...(r || document).querySelectorAll(s)];
const post = m => { try { window.chrome.webview.postMessage(m); } catch (e) {} };
const HYBRID = new URLSearchParams(location.search).get('hybrid') === '1';
const ROOT = 'https://halo.classic/';
const curve = p => Math.pow(Math.max(0, Math.min(100, p)) / 100, 2);

const OFFICIAL = [
	{ id: 'mcc', t: 'Halo: The Master Chief Collection', opts: ['Steam', 'Steam (no anti-cheat)', 'Xbox / Microsoft Store'], icon: 1 },
	{ id: 'h5forge', t: 'Halo 5: Forge', opts: ['Microsoft Store'], icon: 4 },
	{ id: 'infinite', t: 'Halo Infinite', opts: ['Steam', 'Microsoft Store'], icon: 3 },
	{ id: 'ce2026', t: 'Halo: Campaign Evolved (2026)', opts: ['Steam', 'Microsoft Store'], icon: 0 },
	{ id: 'hwde', t: 'Halo Wars: Definitive Edition', opts: ['Steam', 'Microsoft Store'], icon: 7 },
	{ id: 'hw2', t: 'Halo Wars 2', opts: ['Microsoft Store'], icon: 8 },
	{ id: 'spartanassault', t: 'Halo: Spartan Assault', opts: ['Steam', 'Microsoft Store'], icon: 6 },
	{ id: 'spartanstrike', t: 'Halo: Spartan Strike', opts: ['Steam', 'Microsoft Store'], icon: 2 }
].map(g => Object.assign(g, { kind: 'off' }));
const BASE_FAN = [
	{ id: 'reach', t: 'Halo Reach Renaissance', icon: 4, opts: ['Play', 'Locate Game'] },
	{ id: 'oce', t: 'Open Community Edition', icon: 5, opts: ['Play', 'Locate Game'] },
	{ id: 'gravemind', t: 'Halo 2 Gravemind', icon: 10, opts: ['Play', 'Locate Game'] },
	{ id: 'halo3', t: 'Reclaimer', icon: 9, opts: ['Play', 'Locate Game'] },
	{ id: 'groundside', t: 'Project Groundside', icon: 11, opts: ['Install', 'Locate Game'] },
	{ id: 'raven', t: 'Project Raven', icon: 11, opts: ['Play', 'Locate Game'] }
].map(g => Object.assign(g, { kind: 'fan' }));
const ADD_APP = { id: 'addapp', t: 'Add Your Own App', sub: 'Pick a program to add it here', icon: 12, kind: 'fan' };
const MOD_URLS = ['https://steamcommunity.com/app/976730/workshop/', 'https://steamcommunity.com/my/myworkshopfiles/?appid=976730&browsefilter=mysubscriptions'];

let S = {};
let FAN = BASE_FAN.concat([ADD_APP]);
let view = 'home', overlay = null, cur = null, sel = null, counting = false;
let usingPad = false, mouseMode = true, cursorMode = 0, navX = 0, navY = 0, mX = 0, mY = 0;
let camp = null, maps = null, newsHasNew = false;
const lastFocus = {};
const TABS = ['home', 'library', 'news', 'settings', 'credits'];
const bgm = $('#bgm'), bgv = $('#bgv');

const SPR = { back: [0, 168], cursor: [200, 118], fail: [350, 507], fwd: [900, 121] };
let actx = null, abuf = null; const fx = {};
(async function () {
	try {
		actx = new (window.AudioContext || window.webkitAudioContext)();
		const r = await fetch('../assets/ui/sounds.mp3');
		abuf = await actx.decodeAudioData(await r.arrayBuffer());
	} catch (e) { abuf = null; }
	try {
		for (const [k, f] of Object.entries({ cursor: 'move', fwd: 'select', back: 'back', fail: 'fail' })) {
			try { const r2 = await fetch('../assets/ui/sfx-' + f + '.wav'); if (r2.ok) fx[k] = await actx.decodeAudioData(await r2.arrayBuffer()); } catch (e) {}
		}
	} catch (e) {}
})();
function snd(name) {
	const cu = (S.menuSounds || {})[name];
	if (cu) {
		try { const a = new Audio(cu), v = S.vol || {}; a.volume = Math.min(1, curve(v.master == null ? 100 : v.master) * curve(v.menu == null ? 100 : v.menu)); a.play().catch(() => {}); } catch (e) {}
		return;
	}
	if (!actx || (!abuf && !fx[name])) return;
	try {
		if (actx.state === 'suspended') actx.resume();
		const v = S.vol || {}, g = actx.createGain(), src = actx.createBufferSource();
		g.gain.value = curve(v.master == null ? 100 : v.master) * curve(v.menu == null ? 100 : v.menu);
		g.connect(actx.destination); src.connect(g);
		if (abuf) { src.buffer = abuf; src.start(0, SPR[name][0] / 1000, SPR[name][1] / 1000); }
		else { src.buffer = fx[name]; src.start(); }
	} catch (e) {}
}

function loadImage(u) { return new Promise((res, rej) => { const i = new Image(); i.crossOrigin = 'anonymous'; i.onload = () => res(i); i.onerror = rej; i.src = u; }); }
async function slices(url, n, heights) {
	const img = await loadImage(url), out = [];
	const hs = heights || Array(n).fill(img.height / n);
	let y = 0;
	for (const h of hs) {
		const c = document.createElement('canvas'); c.width = img.width; c.height = h;
		c.getContext('2d').drawImage(img, 0, -y, img.width, img.height);
		out.push(c.toDataURL('image/jpeg', 0.9)); y += h;
	}
	return out;
}
slices('../assets/ui/campaign.jpg', 10).then(u => { camp = u; paintAll(); }).catch(() => {});
slices('../assets/ui/maps.jpg', 1, [360, 400, 400, 400, 400, 400, 442, 480, 512, 512, 512, 512, 512]).then(u => { maps = u; paintAll(); }).catch(() => {});

function artUrl(t) {
	const l = (S.logos || {})[t.id];
	if (l) return new URL(l, ROOT).href;
	return (t.kind === 'off' ? camp : maps) ? (t.kind === 'off' ? camp : maps)[t.icon] || '' : '';
}

function statusOf(t) {
	if (t.kind === 'off') {
		const st = (S.official || {})[t.id] || [], loc = (S.located || {})[t.id];
		if (loc) return { text: 'Installed: your copy', cls: 'ok' };
		if (st.some(x => x === 'Installed')) return { text: 'Installed', cls: 'ok' };
		if (st.length && st.every(x => x === 'Not installed')) return { text: 'Not installed', cls: 'no' };
		return { text: t.opts.length > 1 ? t.opts.length + ' ways to play' : t.opts[0], cls: '' };
	}
	let sub = t.id === 'reach' ? S.reach : t.id === 'halo3' ? S.halo3 : t.id === 'gravemind' ? S.gravemind : t.id === 'groundside' ? S.groundside : t.id === 'oce' ? S.oce : t.id === 'raven' ? S.raven : t.sub;
	sub = sub || '';
	return { text: sub, cls: /^(Ready|Installed)/i.test(sub) ? 'ok' : /^(Not installed|File missing)/i.test(sub) ? 'no' : '' };
}

function mkTile(t) {
	const a = document.createElement('a');
	a.className = 'fx tile'; a._t = t; a.dataset.id = t.id;
	a.innerHTML = '<div class="art"></div><div class="initial"></div><div class="cap"><b></b><i></i></div>';
	a.onclick = () => openDetail(a._t);
	return a;
}
function hue(id) { let h = 0; for (const c of id) h = (h * 31 + c.charCodeAt(0)) % 360; return h; }
function paintTile(a) {
	const t = a._t, u = artUrl(t), st = statusOf(t);
	$('.art', a).style.backgroundImage = u ? 'url("' + u + '")' : '';
	a.style.background = u ? '' : 'linear-gradient(135deg,hsl(' + hue(t.id) + ',52%,30%),hsl(' + (hue(t.id) + 35) + ',58%,13%))';
	$('b', a).textContent = t.t;
	const i = $('i', a); i.textContent = st.text; i.className = st.cls;
}
function visibleOfficial() {
	const st = S.official || {};
	const vis = OFFICIAL.filter(g => !(S.hide && (st[g.id] || []).length && st[g.id].every(x => x === 'Not installed') && !(S.located || {})[g.id]));
	return vis.length ? vis : OFFICIAL;
}
let sig = '';
function renderTiles() {
	const fan = FAN, off = visibleOfficial();
	const nsig = JSON.stringify([fan.map(t => t.id + t.t), off.map(t => t.id)]);
	if (nsig !== sig) {
		sig = nsig;
		const keep = cur && cur._t ? cur._t.id : null, keepIn = cur && cur.parentElement ? cur.parentElement.id : null;
		const fill = (el, list) => { el.innerHTML = ''; list.forEach(t => el.appendChild(mkTile(t))); };
		fill($('#shelfFan'), fan); fill($('#shelfOff'), off); fill($('#gridAll'), fan.concat(off));
		if (keep) {
			const again = $$('.tile', $('#' + keepIn) || document).find(e => e._t.id === keep);
			if (again) setFocus(again, false);
		}
	}
	paintAll();
}
function paintAll() { $$('.tile').forEach(paintTile); if (sel) updateHero(sel); }

function updateHero(t) {
	sel = t;
	const st = statusOf(t);
	$('#heroTitle').textContent = t.t;
	$('#heroSub').innerHTML = '<span class="' + st.cls + '">' + esc(st.text) + '</span>';
	const u = artUrl(t), b = $('#bgart');
	b.style.backgroundImage = u ? 'url("' + u + '")' : '';
	b.classList.toggle('on', !!u);
}
function esc(t) { return String(t).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }

function actionsFor(t) {
	const list = [];
	if (t.kind === 'off') {
		const st = (S.official || {})[t.id] || [], loc = (S.located || {})[t.id];
		t.opts.forEach((o, i) => list.push({ label: o, small: st[i] || '', primary: i === 0, run: () => post({ type: 'open', game: t.id, option: i }) }));
		let n = t.opts.length;
		if (loc) { const k = n; list.push({ label: 'My Copy', small: loc, run: () => post({ type: 'open', game: t.id, option: k }) }); n++; }
		const k2 = n;
		list.push({ label: 'Locate Game', run: () => post({ type: 'open', game: t.id, option: k2 }) });
		if (t.id === 'mcc') {
			list.push({ label: 'Install Mods', run: () => post({ type: 'link', url: MOD_URLS[0] }) });
			list.push({ label: 'Manage My Mods', run: () => post({ type: 'link', url: MOD_URLS[1] }) });
		}
	} else if (t.opts) {
		t.opts.forEach((o, i) => list.push({ label: o, primary: i === 0, run: () => post({ type: 'open', game: t.id, option: i }) }));
	} else {
		list.push({ label: t.id === 'addapp' ? 'Add App' : 'Open', primary: true, run: () => post({ type: 'fan', id: t.id }) });
	}
	return list;
}
function launchDefault(t) { if (!t) return; const a = actionsFor(t)[0]; if (a) a.run(); }

function openOverlay(el) {
	closeOverlay(true);
	overlay = { el, prev: cur };
	el.classList.add('on');
	const first = $$('.fx', el)[0];
	if (first) setFocus(first, false);
	hints();
}
function closeOverlay(quiet) {
	if (!overlay) return;
	overlay.el.classList.remove('on'); overlay.el.innerHTML = '';
	const prev = overlay.prev; overlay = null;
	if (!quiet && prev && document.contains(prev)) setFocus(prev, false);
	hints();
}
function openDetail(t) {
	if (counting) return;
	const el = $('#detail'), st = statusOf(t), u = artUrl(t);
	el.innerHTML = '<div class="sheet"><div class="dArt"></div><div class="dInfo"><h2></h2><p class="st"></p><div class="acts"></div></div></div>';
	if (u) $('.dArt', el).style.backgroundImage = 'url("' + u + '")';
	$('h2', el).textContent = t.t;
	$('.st', el).innerHTML = '<span class="' + st.cls + '">' + esc(st.text) + '</span>' + (t.kind === 'off' && (S.played || {})[t.id] ? '<br><span>' + esc(S.played[t.id]) + '</span>' : '');
	actionsFor(t).forEach(a => {
		const b = document.createElement('a');
		b.className = 'fx act' + (a.primary ? ' primary' : '');
		b.innerHTML = '<span></span><small></small>';
		$('span', b).textContent = a.label; $('small', b).textContent = a.small || '';
		b.onclick = () => { closeOverlay(true); a.run(); };
		$('.acts', el).appendChild(b);
	});
	el.onclick = e => { if (e.target === el) closeOverlay(); };
	openOverlay(el);
}
function openPower() {
	const el = $('#power');
	el.innerHTML = '<div class="sheet power"><h2>Power</h2><div class="acts"></div></div>';
	const items = [
		['Resume', () => {}],
		['Minimize', () => post({ type: 'minimize' })],
		[S.window ? 'Go fullscreen' : 'Go windowed', () => post({ type: 'setWindow', value: S.window ? 0 : 1 })],
		['Exit launcher', () => post({ type: 'exit' })]
	];
	items.forEach((it, i) => {
		const b = document.createElement('a');
		b.className = 'fx act' + (i === 0 ? ' primary' : '');
		b.innerHTML = '<span></span>'; $('span', b).textContent = it[0];
		b.onclick = () => { closeOverlay(true); it[1](); };
		$('.acts', el).appendChild(b);
	});
	el.onclick = e => { if (e.target === el) closeOverlay(); };
	openOverlay(el);
}

function openDrop(r) {
	const d = r._d, el = $('#drop');
	el.innerHTML = '<div class="sheet drop"><h2></h2><div class="acts"></div></div>';
	$('h2', el).textContent = d.label;
	let first = null;
	(d.drop || []).forEach((text, k) => {
		const now = text === d.value;
		const b = document.createElement('a');
		b.className = 'fx act' + (now ? ' primary' : '');
		b.innerHTML = '<span></span><small></small>'; $('span', b).textContent = text; $('small', b).textContent = now ? '\u2713' : '';
		b.onclick = () => { closeOverlay(); if (!now) { post({ type: 'settingPick', i: d.i, v: k }); snd('fwd'); } };
		$('.acts', el).appendChild(b);
		if (now) first = b;
	});
	el.onclick = e => { if (e.target === el) closeOverlay(); };
	openOverlay(el);
	if (first) setFocus(first, false);
}

function goto(v, keepFocus) {
	if (!TABS.includes(v)) return;
	closeOverlay(true);
	view = v;
	$$('.view').forEach(e => e.classList.toggle('on', e.id === v));
	$$('.tab[data-view]').forEach(e => e.classList.toggle('on', e.dataset.view === v));
	if (v === 'news') { post({ type: 'newsSeen' }); newsHasNew = false; $('#newDot').classList.remove('on'); }
	if (!keepFocus) {
		const f = lastFocus[v] && document.contains(lastFocus[v]) ? lastFocus[v] : $$('.fx', $('#' + v)).find(visible);
		if (f) setFocus(f, false);
	}
	hints();
}

function visible(e) { return e.getClientRects().length > 0; }
function scope() {
	if (overlay) return $$('.fx', overlay.el).filter(visible);
	return $$('#top .fx').concat($$('.fx', $('#' + view))).filter(visible);
}
function setFocus(el, scroll) {
	if (cur) cur.classList.remove('focus');
	cur = el;
	if (!el) return;
	el.classList.add('focus');
	if (scroll !== false) { try { el.scrollIntoView({ block: 'nearest', inline: 'center', behavior: 'smooth' }); } catch (e) {} }
	if (el._t) updateHero(el._t);
	if (el._cat != null && el._cat !== bpCat) showCat(el._cat);
	if (!overlay && el.closest('.view')) lastFocus[view] = el;
	hints();
}
function move(dir) {
	const list = scope();
	if (!cur || !list.includes(cur)) { setFocus(list[0]); return; }
	const r0 = cur.getBoundingClientRect(), cx = r0.left + r0.width / 2, cy = r0.top + r0.height / 2;
	let best = null, bs = 1e9;
	for (const e of list) {
		if (e === cur) continue;
		const r = e.getBoundingClientRect(), x = r.left + r.width / 2 - cx, y = r.top + r.height / 2 - cy;
		let p, s;
		if (dir === 'right') { if (x < 6) continue; p = x; s = Math.abs(y); }
		else if (dir === 'left') { if (x > -6) continue; p = -x; s = Math.abs(y); }
		else if (dir === 'down') { if (y < 6) continue; p = y; s = Math.abs(x); }
		else { if (y > -6) continue; p = -y; s = Math.abs(x); }
		const score = p + s * 2.4;
		if (score < bs) { bs = score; best = e; }
	}
	if (best) { setFocus(best); snd('cursor'); }
	else if (!overlay && (dir === 'down' || dir === 'up')) { const v = $('#' + view); v.scrollBy({ top: dir === 'down' ? 140 : -140, behavior: 'smooth' }); }
}

const act = (action, target) => post({ type: 'act', action, target: target || '' });
function blurOn() { return S.blur !== false; }
function applyBlur() { document.body.classList.toggle('sharp', !HYBRID && !blurOn()); }
let bpCat = 0;
const SETMODEL = () => S.settings || [];
function showCat(i) {
	if (i === bpCat && $('#setList').childElementCount) return;
	bpCat = i;
	$$('.setCat').forEach((a, k) => a.classList.toggle('on', k === i));
	fillSetRows();
}
function fillSetRows() {
	const box = $('#setList'), c = SETMODEL()[bpCat];
	box.innerHTML = '';
	if (!c) return;
	c.items.forEach(d => {
		const r = document.createElement('div'); r.className = 'setRow'; r.dataset.i = d.i;
		r.innerHTML = '<div class="l"><b></b><span></span></div><div class="v"></div>';
		$('b', r).textContent = d.label; $('span', r).textContent = d.desc;
		const v = $('.v', r), hasVal = d.value && d.value !== '-';
		if (d.kind === 'btns' || d.kind === 'btn') {
			v.className = d.kind === 'btns' ? 'v two' : 'v one';
			v.innerHTML = '<i class="val"></i>'; $('.val', v).textContent = hasVal ? d.value : '';
			(d.btns || []).forEach((t, k) => {
				const b = document.createElement('a'); b.className = 'fx sbtn'; b.textContent = t; b.dataset.sub = k;
				b.onclick = e => { e.stopPropagation(); snd('fwd'); post({ type: 'setting', i: d.i, d: k ? -1 : 1 }); };
				v.appendChild(b);
			});
		} else {
			r.classList.add('fx'); r._d = d;
			if (d.kind === 'toggle') {
				r.classList.add('chkRow'); if (d.on) r.classList.add('on');
				v.className = 'v chk'; v.innerHTML = '<span class="box"></span>'; $('.box', v).textContent = d.on ? '\u2713' : '';
			} else { v.className = d.kind === 'drop' ? 'v dd' : 'v'; v.textContent = d.value; }
			r.onclick = () => (d.kind === 'drop' ? openDrop(r) : change(r, 1));
			r.oncontextmenu = e => { e.preventDefault(); if (d.kind !== 'toggle') change(r, -1); };
		}
		box.appendChild(r);
	});
}
function buildSettings() {
	const cats = SETMODEL(), cbox = $('#setCats'), box = $('#setList');
	let keep = null;
	if (cur && $('#settings').contains(cur)) {
		const row = cur.closest ? cur.closest('.setRow') : null;
		keep = { cat: cur.classList.contains('setCat') ? cur.dataset.c : null, i: row ? row.dataset.i : null, sub: cur.classList.contains('sbtn') ? cur.dataset.sub : null };
	}
	cbox.innerHTML = ''; box.innerHTML = '';
	if (!cats.length) return;
	bpCat = Math.max(0, Math.min(bpCat, cats.length - 1));
	cats.forEach((c, i) => {
		const a = document.createElement('a'); a.className = 'fx setCat' + (i === bpCat ? ' on' : '');
		a.textContent = c.name; a.dataset.c = i; a._cat = i;
		a.onclick = () => { showCat(i); const f = $$('.fx', box).find(visible); if (f) setFocus(f, false); };
		cbox.appendChild(a);
	});
	fillSetRows();
	if (keep) {
		let t = null;
		if (keep.cat != null) t = $$('.setCat')[+keep.cat];
		else if (keep.i != null) {
			const row = $('.setRow[data-i="' + keep.i + '"]', box);
			if (row) t = keep.sub != null ? $('.sbtn[data-sub="' + keep.sub + '"]', row) : row;
		}
		if (t) setFocus(t, false);
	}
}
function change(r, delta) {
	post({ type: 'setting', i: r._d.i, d: delta });
	snd('cursor');
}

function renderNews(m) {
	const box = $('#newsList'), items = m.items || [];
	if (!items.length) { box.innerHTML = '<div class="note">' + (m.total ? 'All news sources are turned off. You can turn them on in Settings.' : "Couldn't load the latest release notes. Check your internet connection.") + '</div>'; return; }
	box.innerHTML = '';
	items.forEach(x => {
		const c = document.createElement('div'); c.className = 'card';
		c.innerHTML = '<h3><span>' + esc(x.name) + '</span><span>' + esc(x.label) + (x.isNew ? '<span class="new">NEW</span>' : '') + '</span></h3><div class="meta">' + esc(x.date || '') + '</div><div class="body">' + esc(x.notes || '') + '</div>';
		if (x.url) {
			const a = document.createElement('a'); a.className = 'fx link'; a.textContent = 'View on GitHub';
			a.onclick = () => post({ type: 'link', url: x.url }); c.appendChild(a);
		}
		box.appendChild(c);
	});
	newsHasNew = items.some(x => x.isNew);
	$('#newDot').classList.toggle('on', newsHasNew && view !== 'news');
}

function setCursorMode(m) {
	cursorMode = m;
	document.body.classList.toggle('noCursor', m === 1);
	if (m !== 0) document.body.classList.remove('padMode');
}
function enterNav(fromPad) {
	if (mouseMode) { mouseMode = false; navX = mX; navY = mY; }
	if (fromPad) { if (!usingPad) { usingPad = true; hints(); } if (cursorMode === 0) document.body.classList.add('padMode'); }
}
function press(code, fromPad) {
	if (counting) return;
	enterNav(!!fromPad);
	const dirs = { ArrowUp: 'up', ArrowDown: 'down', ArrowLeft: 'left', ArrowRight: 'right' };
	if (dirs[code]) {
		const d = dirs[code];
		if (!overlay && cur && cur._d && (d === 'left' || d === 'right')) { change(cur, d === 'right' ? 1 : -1); return; }
		if (!overlay && cur && cur.classList.contains('setCat') && (d === 'up' || d === 'down')) {
			const cs = $$('.setCat'), at = cs.indexOf(cur), to = d === 'down' ? at + 1 : at - 1;
			if (to >= 0 && to < cs.length) { setFocus(cs[to], false); snd('cursor'); return; }
			if (d === 'down') return;
		}
		move(d); return;
	}
	if (code === 'Enter' || code === 'NumpadEnter' || code === 'Space') { if (cur) cur.click(); return; }
	if (code === 'Escape' || code === 'Backspace') {
		if (overlay) { closeOverlay(); snd('back'); }
		else if (view === 'settings' && cur && cur.classList.contains('fx') && !cur.classList.contains('setCat') && $('#settings').contains(cur)) { const c = $$('.setCat')[bpCat]; if (c) { setFocus(c, false); snd('back'); } }
		else if (view !== 'home') { goto('home'); snd('back'); }
		return;
	}
	if (code === 'Start') { if (overlay && overlay.el.id === 'power') { closeOverlay(); snd('back'); } else { openPower(); snd('fwd'); } return; }
	if ((code === 'KeyQ' || code === 'KeyE') && !overlay) {
		const i = TABS.indexOf(view), n = TABS.length;
		const t = TABS[(i + (code === 'KeyE' ? 1 : n - 1)) % n];
		goto(t, true); snd('cursor');
		const tab = $('.tab[data-view="' + t + '"]'); if (tab) setFocus(tab, false);
	}
}
window.addEventListener('keydown', e => {
	if (dlOn && (e.key === 'Escape' || e.key === 'Backspace')) { post({ type: 'cancelDl' }); e.preventDefault(); return; }
	if (e.code === 'Tab') { e.preventDefault(); press('Start', false); return; }
	if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Space', 'Enter', 'Escape', 'Backspace', 'KeyQ', 'KeyE'].includes(e.code)) { e.preventDefault(); press(e.code, false); }
});
window.addEventListener('mousemove', e => {
	mX = e.clientX; mY = e.clientY;
	if (!mouseMode && Math.hypot(mX - navX, mY - navY) > 6) { mouseMode = true; document.body.classList.remove('padMode'); post({ type: 'mouse' }); }
}, true);
window.addEventListener('mousedown', e => {
	if (e.button === 0 && e.target && (e.target.id === 'dragBar' || (S.window && e.target.closest && e.target.closest('#top') && !e.target.closest('.fx, .tab, a, button, input')))) post({ type: 'drag' });
	if (!mouseMode) { mouseMode = true; document.body.classList.remove('padMode'); post({ type: 'mouse' }); }
}, true);
document.addEventListener('mouseover', e => {
	if (!mouseMode || counting) return;
	const el = e.target.closest && e.target.closest('.fx');
	if (el && visible(el) && el !== cur && (!overlay || overlay.el.contains(el))) { setFocus(el, false); snd('cursor'); }
});

$$('.tab[data-view]').forEach(t => { t.onclick = () => goto(t.dataset.view, true); });
$$('.tab[data-act="chat"]').forEach(t => { t.onclick = () => post({ type: 'chat' }); });
$('#minBtn').onclick = () => post({ type: 'minimize' });
$('#winBtn').onclick = () => post({ type: 'setWindow', value: S.window ? 0 : 1 });
$('#powerBtn').onclick = openPower;
$('#heroPlay').onclick = () => launchDefault(sel);
document.addEventListener('click', e => {
	if (e.target.closest && e.target.closest('.fx') && !e.target.closest('.setRow')) snd('fwd');
	const a = e.target.closest && e.target.closest('a[data-url]');
	if (a) post({ type: 'link', url: a.dataset.url });
	if (e.target.closest && e.target.closest('a[data-mail]')) post({ type: 'mail' });
});
function tick() { $('#clock').textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
tick(); setInterval(tick, 15000);

document.addEventListener('click', e => {
	const b = e.target.closest && e.target.closest('#hints .hb');
	if (b) { snd('fwd'); press(b.dataset.code, false); }
});
function hints() {
	const k = (t, c) => '<span class="key ' + (c || '') + '">' + t + '</span>';
	const ctx = !overlay && cur && cur._d;
	const h = usingPad
		? [['Enter', k('A', 'A') + ' Select'], ['Escape', k('B', 'B') + ' Back'], [null, ctx ? k('&#9664;&#9654;', 'sq') + ' Change' : k('&#9664;&#9654;&#9650;&#9660;', 'sq') + ' Move'], ['KeyE', k('LB', 'sq') + k('RB', 'sq') + ' Tabs'], ['Start', k('&#9776;', 'sq') + ' Power']]
		: [['Enter', k('Enter', 'sq') + ' Select'], ['Escape', k('Esc', 'sq') + ' Back'], [null, ctx ? k('&#9664;&#9654;', 'sq') + ' Change' : k('&#9664;&#9654;&#9650;&#9660;', 'sq') + ' Move'], ['KeyE', k('Q', 'sq') + k('E', 'sq') + ' Tabs'], ['Start', k('Tab', 'sq') + ' Power']];
	$('#hints').innerHTML = h.map(([c, x]) => '<span' + (c ? ' class="hb" data-code="' + c + '"' : '') + '>' + x + '</span>').join('');
	$('#winBtn').textContent = S.window ? '\u26F6' : '\u2750';
	$('#winBtn').title = S.window ? 'Fullscreen' : 'Windowed';
}

let musicOn = true;
function applyVol() { const v = S.vol || {}; bgm.volume = 0.8 * curve(v.master == null ? 100 : v.master) * curve(v.music == null ? 100 : v.music); bgv.volume = bgm.volume; bgv.muted = !S.bgAudio; if (S.bgAudio) bgm.pause(); }
function playMedia() { if (!HYBRID) return; document.body.classList.remove('paused'); try { bgv.play().catch(() => {}); if (musicOn && !S.bgAudio) bgm.play().catch(() => {}); } catch (e) {} }
function pauseMedia() { if (!HYBRID) return; document.body.classList.add('paused'); try { bgv.pause(); bgm.pause(); } catch (e) {} }
if (HYBRID) {
	document.body.classList.add('hybrid');
	bgv.src = '../assets/ui/bg-1080.mp4'; bgm.src = '../assets/ui/bg.mp3';
	applyVol(); playMedia();
	document.addEventListener('visibilitychange', () => { document.hidden ? pauseMedia() : playMedia(); });
	window.addEventListener('blur', () => { if (!chatOpen) pauseMedia(); });
	window.addEventListener('focus', () => { if (!stopped) playMedia(); });
}
let chatOpen = false, stopped = false, dlOn = false;
document.addEventListener('click', ev => { if (ev.target && ev.target.id === 'cancelDl') post({ type: 'cancelDl' }); });

function applyState(s) {
	S = s;
	applyBlur();
	const inst = s.installed || {};
	const rem = s.removable || {};
	BASE_FAN.forEach(x => { if (x.id in inst) x.opts = [inst[x.id] ? 'Play' : 'Install', 'Locate Game'].concat(rem[x.id] ? ['Uninstall'] : []).concat(x.id === 'groundside' && rem[x.id] ? ['Download Again'] : []); });
	const apps = s.apps || [], asig = JSON.stringify(apps);
	if (asig !== applyState.asig) {
		applyState.asig = asig; applyState.rebuild = true;
		FAN = BASE_FAN.concat(apps.map(a => ({ id: 'app:' + a.id, t: a.name, sub: a.ok ? a.file : 'File missing: ' + a.file, icon: 11, opts: ['Play', 'Rename', 'Change Location', 'Change Picture', 'Remove'], kind: 'fan' })), [ADD_APP]);
	}
	setCursorMode(s.cursor || 0);
	$('#dragBar').style.display = s.window ? 'block' : 'none';
	applyVol();
	renderTiles(); applyState.rebuild = false; buildSettings(); hints();
	if (!cur) { const f = $('#shelfFan .tile'); if (f) setFocus(f, false); }
}
if (window.chrome && window.chrome.webview) {
	window.chrome.webview.addEventListener('message', ev => {
		const m = ev.data || {};
		switch (m.type) {
			case 'state': applyState(m); break;
			case 'news': renderNews(m); break;
			case 'busy': $('#status').classList.toggle('on', !!m.busy); $('#statusText').textContent = (m.text || '').trim(); dlOn = !!(m.busy && m.determinate); $('#cancelDl').style.display = dlOn ? 'inline-block' : 'none'; $('#bar').classList.toggle('ind', !!m.busy && !m.determinate); $('#bar i').style.width = '0'; break;
			case 'progress': $('#bar i').style.width = m.value + '%'; break;
			case 'pad': press(m.code, true); break;
			case 'xinput': usingPad = true; hints(); break;
			case 'show': stopped = false; goto('home'); playMedia(); break;
			case 'stop': stopped = true; pauseMedia(); break;
			case 'chatOpen': chatOpen = !!m.value; if (chatOpen && !stopped) playMedia(); break;
			case 'count': {
				counting = !!m.text; const c = $('#countdown');
				if (!m.text) { c.className = ''; c.innerHTML = ''; } else { c.className = 'on' + (m.text === 'GO' ? ' go' : ''); c.innerHTML = '<span>' + esc(m.text) + '</span>'; }
				break;
			}
		}
	});
}
applyBlur(); buildSettings(); renderTiles(); hints();
{ const f = $('#shelfFan .tile'); if (f) setFocus(f, false); }
post({ type: 'ready' });
})();
