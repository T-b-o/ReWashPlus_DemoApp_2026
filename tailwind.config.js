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
                    dark: "#1D1D2D",    // base app background
                    light: "#252540",   // cards, sidebar, elevated surfaces
                    surface: "#2D2D48", // inputs, hover rows
                    border: "#363658",  // dividers
                    text: "#E2E4EF",    // primary text
                    muted: "#8589A5",   // secondary / placeholder text
                    accent: "#7B68EE",  // purple accent
                    green: "#23C8B0",   // success
                    yellow: "#F59E0B",  // warning
                    red: "#F87171",     // danger
                    blue: "#60A5FA",    // info
                },
            },
            borderRadius: {
                modal: "1.25rem",
                xl: "1rem"
            },
            boxShadow: {
                clickup: "0 2px 8px rgba(0,0,0,0.4), 0 0 0 1px rgba(255,255,255,0.04)",
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
                    "@apply px-4 py-2 rounded-lg bg-clickup-surface text-clickup-muted hover:text-clickup-text transition": {},
                },
                // Inputs
                ".input": {
                    "@apply block w-full rounded-lg bg-clickup-surface border border-clickup-border p-3 text-clickup-text placeholder-clickup-muted focus:outline-none focus:ring-2 focus:ring-clickup-accent": {},
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
