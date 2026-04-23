const FAVORITES_STORAGE_KEY = 'devportal_favorites';
const FAVORITES_CATEGORY_TITLE = 'Favorites';

let allTiles = [];
let tagsDescription = {};
let cardElements = [];
let favCardElements = [];
let favorites = new Set(JSON.parse(localStorage.getItem(FAVORITES_STORAGE_KEY) || '[]'));

let favoritesSection = null;
let favoritesGrid = null;

let appElement = null;
let searchInput = null;

function escapeHtml(str) {
    if (!str) return "";
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}

function highlight(text, query) {
    if (!text) return "";
    const escaped = escapeHtml(text);
    if (!query) return escaped;

    const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return escaped.replace(regex, '<mark>$1</mark>');
}

function saveFavorites() {
    localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify([...favorites]));
}

function updateAllStarButtons() {
    document.querySelectorAll('.star-btn').forEach(btn => {
        btn.classList.toggle('favorited', favorites.has(btn.dataset.tile));
    });
}

function toggleFavorite(name) {
    if (favorites.has(name)) {
        favorites.delete(name);
    } else {
        favorites.add(name);
    }
    saveFavorites();
    renderFavoritesGrid();
    updateAllStarButtons();
    handleSearch();
}

function renderTag(tag) {
    const description = tagsDescription[tag];
    return `<span class="tag"${description ? ` title="${escapeHtml(description)}"` : ""}>${escapeHtml(tag)}</span>`;
}

function renderAdditionalLink(link) {
    let iconHtml = "";
    if (link.icon) {
        const iconEscaped = escapeHtml(link.icon);
        iconHtml = `<span class="icon" style="mask-image: url(${iconEscaped}); -webkit-mask-image: url(${iconEscaped});${link.iconColor ? ` color: ${escapeHtml(link.iconColor)}` : ""}"></span>`;
    }
    return `<a href="${escapeHtml(link.url)}" class="secondary-link" target="_blank" rel="noopener noreferrer">
                ${iconHtml}
                <span>${escapeHtml(link.title)}</span>
            </a>`;
}

function createCardElement(tile) {
    const card = document.createElement("div");
    card.className = "card";

    const width = tile.width || 3;
    const height = tile.height || 3;
    if (width > 1) card.style.gridColumn = `span ${width}`;
    if (height > 1) card.style.gridRow = `span ${height}`;

    const hasPrimary = tile.main && tile.main.url;

    if (hasPrimary) card.classList.add("has-primary");

    const mainIconBgStyle = tile.main.iconBg ? ` style="background-color: ${escapeHtml(tile.main.iconBg)}"` : "";
    card.innerHTML = `
        ${hasPrimary ? `<a href="${escapeHtml(tile.main.url)}" class="card-link" target="_blank" rel="noopener noreferrer" aria-label="Open ${escapeHtml(tile.main.title)}"></a>` : ""}
        <div class="card-header">
            <div class="card-icon"${mainIconBgStyle}>
                ${tile.main.icon ? `<img class="icon" src="${escapeHtml(tile.main.icon)}" alt="">` : `<img class="icon-default-color" src="./icons/cube.svg" alt="">`}
            </div>
            <div class="card-title-area">
                <h3 class="card-title" data-field="title">${escapeHtml(tile.main.title)}</h3>
            </div>
        </div>
        <p class="card-description" data-field="description">${escapeHtml(tile.description)}</p>
        ${tile.tags && tile.tags.length > 0 ? `
        <div class="tag-list" data-field="tags">
            ${tile.tags.map(renderTag).join("")}
        </div>` : ""}
        ${tile.additionalLinks && tile.additionalLinks.length > 0 ? `
        <div class="card-actions">
            <div class="secondary-links" data-field="links">
                ${tile.additionalLinks.map(renderAdditionalLink).join("")}
            </div>
        </div>` : ""}
    `;
    
    const starBtn = document.createElement('button');
    starBtn.type = 'button';
    starBtn.className = 'star-btn' + (favorites.has(tile.main.title) ? ' favorited' : '');
    starBtn.dataset.tile = tile.main.title;
    starBtn.setAttribute('aria-label', 'Toggle favorite');
    starBtn.innerHTML = '<span class="star-icon"></span>';
    starBtn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        toggleFavorite(tile.main.title);
    });
    card.appendChild(starBtn);

    return card;
}

function renderFavoritesGrid() {
    if (!favoritesGrid || !favoritesSection) return;
    favoritesGrid.innerHTML = '';
    favCardElements = [];

    allTiles.filter(t => favorites.has(t.main.title)).forEach(tile => {
        const card = createCardElement(tile);
        favoritesGrid.appendChild(card);
        favCardElements.push({element: card, tile, section: favoritesSection});
    });

    favoritesSection.classList.toggle('hidden', favCardElements.length === 0);
}

async function loadCatalog() {
    try {
        const res = await fetch("/api/tiles");
        if (!res.ok) throw new Error(`Failed to load catalog: ${res.statusText}`);
        const data = await res.json();
        const categories = data.categories || [];
        allTiles = categories.flatMap(c => c.tiles || []);
        tagsDescription = data.tagsDescription || {};
        initialRender(categories);
    } catch (err) {
        console.error(err);
        appElement.innerHTML = `<div class="error">Error: ${escapeHtml(err.message)}</div>`;
    }
}

function initialRender(categories) {
    appElement.innerHTML = "";
    cardElements = [];
    favCardElements = [];
    favoritesSection = null;
    favoritesGrid = null;

    if (categories.length === 0) {
        appElement.innerHTML = `<div class="no-results">No services found.</div>`;
        return;
    }

    const container = document.createDocumentFragment();

    favoritesSection = document.createElement("section");
    favoritesSection.className = "category-section";
    favoritesSection.dataset.category = FAVORITES_CATEGORY_TITLE;

    const favHeader = document.createElement("h2");
    favHeader.className = "category-title";
    favHeader.textContent = FAVORITES_CATEGORY_TITLE;
    favoritesSection.appendChild(favHeader);

    favoritesGrid = document.createElement("div");
    favoritesGrid.className = "card-grid card-grid__favorites";
    favoritesSection.appendChild(favoritesGrid);

    container.appendChild(favoritesSection);

    [...categories].sort((a, b) => (a.priority ?? 0) - (b.priority ?? 0)).forEach(category => {
        const section = document.createElement("section");
        section.className = "category-section";
        section.dataset.category = category.name;

        const header = document.createElement("h2");
        header.className = "category-title";
        header.textContent = category.name;
        section.appendChild(header);

        const grid = document.createElement("div");
        grid.className = "card-grid";

        (category.tiles || []).forEach(tile => {
            const card = createCardElement(tile);
            grid.appendChild(card);
            cardElements.push({element: card, tile, section});
        });

        section.appendChild(grid);
        container.appendChild(section);
    });

    appElement.appendChild(container);

    renderFavoritesGrid();

    const noResults = document.createElement("div");
    noResults.id = "no-results";
    noResults.className = "no-results hidden";
    noResults.textContent = "No services found matching your search.";
    appElement.appendChild(noResults);
}

function handleSearch() {
    const query = searchInput.value.toLowerCase().trim();
    const visibleSections = new Set();
    let visibleCount = 0;

    [...cardElements, ...favCardElements].forEach(({element, tile, section}) => {
        const matchesName = tile.main.title.toLowerCase().includes(query);
        const matchesDesc = tile.description.toLowerCase().includes(query);
        const matchesTags = tile.tags && tile.tags.some(tag => tag.toLowerCase().includes(query));
        const matchesAliases = tile.aliases && tile.aliases.some(alias => alias.toLowerCase().includes(query));
        const matchesLinks = tile.additionalLinks && tile.additionalLinks.some(link =>
            link.title.toLowerCase().includes(query)
        );

        const isVisible = !query || matchesName || matchesDesc || matchesTags || matchesAliases || matchesLinks;

        element.classList.toggle("hidden", !isVisible);

        if (isVisible) {
            visibleCount++;
            visibleSections.add(section);

            element.querySelector('[data-field="title"]').innerHTML = highlight(tile.main.title, query);
            element.querySelector('[data-field="description"]').innerHTML = highlight(tile.description, query);

            const tagList = element.querySelector('[data-field="tags"]');
            if (tagList) {
                tagList.innerHTML = (tile.tags || []).map(tag => {
                    const description = tagsDescription[tag];
                    const titleAttr = description ? ` title="${escapeHtml(description)}"` : "";
                    return `<span class="tag"${titleAttr}>${highlight(tag, query)}</span>`;
                }).join("");
            }

            const linkList = element.querySelector('[data-field="links"]');
            if (linkList) {
                linkList.innerHTML = (tile.additionalLinks || []).map(link => {
                    const iconStyle = link.icon ? `style="mask-image: url(${escapeHtml(link.icon)}); -webkit-mask-image: url(${escapeHtml(link.icon)}); ${link.iconColor ? `color: ${escapeHtml(link.iconColor)}` : ""}"` : "";
                    return `
                        <a href="${escapeHtml(link.url)}" class="secondary-link" title="${escapeHtml(link.title)}" target="_blank" rel="noopener noreferrer">
                            ${link.icon ? `<span class="icon" ${iconStyle}></span>` : ""}
                            <span>${highlight(link.title, query)}</span>
                        </a>
                    `;
                }).join("");
            }
        }
    });

    document.querySelectorAll(".category-section").forEach(section => {
        section.classList.toggle("hidden", !visibleSections.has(section));
    });

    if (favoritesSection && favCardElements.length === 0) {
        favoritesSection.classList.add("hidden");
    }

    const noResults = document.getElementById("no-results");
    if (noResults) {
        noResults.classList.toggle("hidden", visibleCount > 0);
    }
}

let searchTimeout;
document.addEventListener('DOMContentLoaded', () => {
    appElement = document.getElementById("app");
    if (appElement === null) {
        console.error("App element not found.")
        return;
    }

    searchInput = document.getElementById("search-input");
    if (searchInput === null) {
        console.error("Search input not found.")
        return;
    }

    searchInput.addEventListener("input", () => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(handleSearch, 150);
    });

    document.addEventListener('keydown', (e) => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;
        if (e.key.length !== 1) return;
        const active = document.activeElement;
        if (active === searchInput) return;
        searchInput.focus();
    });

    loadCatalog()
});