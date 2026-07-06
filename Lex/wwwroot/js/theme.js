window.themeManager = {
    // Получить системные предпочтения темы
    getSystemTheme: function() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
            return 'light';
        }
        return 'dark'; // по умолчанию
    },

    // Применить тему к документу
    applyTheme: function(isDark) {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');

        // Меняем meta-тег для цвета темы в браузере
        const metaThemeColor = document.querySelector('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.setAttribute('content', isDark ? '#0f172a' : '#ffffff');
        }

        // Сохраняем в localStorage для быстрого доступа из JS
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    },

    // Слушатель изменения системной темы
    listenSystemThemeChanges: function(dotNetHelper) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
            dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', e.matches);
        });
    },

    // Получить сохраненную тему
    getStoredTheme: function() {
        return localStorage.getItem('theme') || 'dark';
    }
};