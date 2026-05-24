/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.razor",
        "./**/*.cshtml",
        "./**/*.html"
    ],
    darkMode: "class",
    theme: {
        extend: {
            colors: {
                clickup: {
                    dark: "#0E0E10",
                    light: "#17171A",
                    border: "#2C2C2C",
                    text: "#E6E7EA",
                    muted: "#9AA0B4",
                    accent: "#7B68EE",
                    green: "#2DD4BF", // success accent
                    yellow: "#FBBF24", // warning accent
                    red: "#FF6B6B", // error accent
                    blue: "#3B82F6", // info accent
                    panel: "#0F1724"
                },
            },
            borderRadius: {
                modal: "1.25rem",
                xl: "1rem"
            },
            boxShadow: {
                clickup: "0 6px 24px rgba(2,6,23,0.6)",
            },
            keyframes: {
                fadeInScale: {
                    "0%": { opacity: "0", transform: "scale(0.98)" },
                    "100%": { opacity: "1", transform: "scale(1)" },
                },
            },
            animation: {
                modal: "fadeInScale 0.25s ease-out forwards",
            },
            backgroundImage: {
                'clickup-sidebar': 'linear-gradient(180deg, #0E0E10 0%, #17171A 100%)',
                'clickup-main': 'linear-gradient(180deg, #0E0E10 0%, #0F1724 100%)',
                'clickup-card': 'linear-gradient(180deg, #17171A 0%, #2C2C2C 100%)',
            },
        },
    },
    plugins: [
        function ({ addComponents }) {
            addComponents({
                // Modal container
                ".modal": {
                    "@apply bg-clickup-light rounded-modal shadow-clickup p-6 animate-modal": {},
                },
                // Modal header
                ".modal-header": {
                    "@apply flex justify-between items-center border-b border-clickup-border pb-3 mb-4": {},
                },
                ".modal-title": {
                    "@apply text-xl font-semibold text-clickup-text": {},
                },
                // Modal body
                ".modal-body": {
                    "@apply space-y-4 text-clickup-text": {},
                },
                // Modal footer
                ".modal-footer": {
                    "@apply flex justify-end space-x-3 mt-6": {},
                },
                // Buttons
                ".btn-primary": {
                    "@apply px-4 py-2 rounded-lg bg-clickup-accent text-white hover:opacity-90 transition": {},
                },
                ".btn-secondary": {
                    "@apply px-4 py-2 rounded-lg bg-clickup-dark text-clickup-muted hover:text-clickup-text transition": {},
                },
                // Inputs
                ".input": {
                    "@apply w-full rounded-lg bg-clickup-dark border border-clickup-border p-3 text-clickup-text placeholder-clickup-muted focus:outline-none focus:ring-2 focus:ring-clickup-accent": {},
                },
                // Badges / status chips
                ".status-badge": {
                    "@apply inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold": {},
                },
                ".status-waiting": {
                    "@apply bg-clickup-yellow text-black": {},
                },
                ".status-inprogress": {
                    "@apply bg-clickup-blue text-white": {},
                },
                ".status-completed": {
                    "@apply bg-clickup-green text-black": {},
                },
                ".status-cancelled": {
                    "@apply bg-clickup-red text-white": {},
                },
            });
        },
    ],
};
