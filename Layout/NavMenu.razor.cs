using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Generic;

namespace ReWashPlus_DemoApp.Layout
{
    public partial class NavMenu : LayoutComponentBase
    {
        protected bool isCollapsed = false;
        protected bool isMobileMenuOpen = false;

        protected List<(string Label, string Icon, string Href, NavLinkMatch Match)> UserLinks = new()
        {
            ("Home", "fas fa-house", "/", NavLinkMatch.All),
            ("Book a Wash", "fas fa-calendar-plus", "/book", NavLinkMatch.Prefix),
            ("My Bookings", "fas fa-list", "/my-bookings", NavLinkMatch.Prefix),
            ("Payments", "fas fa-credit-card", "/payments", NavLinkMatch.Prefix),
            ("Notifications", "fas fa-bell", "/notifications", NavLinkMatch.Prefix),
            ("Profile", "fas fa-user", "/profile", NavLinkMatch.Prefix)
        };

        protected List<(string Label, string Icon, string Href, NavLinkMatch Match)> AdminLinks = new()
        {
            ("Dashboard", "fas fa-tachometer-alt", "/admin/dashboard", NavLinkMatch.Prefix),
            ("Manage Bookings", "fas fa-calendar-check", "/admin/manage-bookings", NavLinkMatch.Prefix),
            ("Manage Payments", "fas fa-wallet", "/admin/manage-payments", NavLinkMatch.Prefix),
            ("Users", "fas fa-users", "/admin/users", NavLinkMatch.Prefix),
            ("Reports", "fas fa-chart-line", "/admin/reports", NavLinkMatch.Prefix),
            ("Settings", "fas fa-cogs", "/admin/settings", NavLinkMatch.Prefix)
        };

        protected void ToggleCollapse() => isCollapsed = !isCollapsed;
        protected void ToggleMobileMenu() => isMobileMenuOpen = !isMobileMenuOpen;
    }
}
