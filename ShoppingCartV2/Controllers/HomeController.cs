/*
 * Course: CS 3410
 * Section: 01
 * Name: Zaid Khan
 * Professor: Emin Mary Abraham
 * Assignment #: Module Assignment #8
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShoppingCartV2.Models;

namespace ShoppingCartV2.Controllers
{
    public partial class HomeController : Controller
    {
        // Text for Site Heading
        string siteHeading = "Slightly Expensive Clothing Store";

        // Text for Order View Heading
        string orderHeading = "Clothe Orders";

        // Text for View Heading for each Tab
        string[] tabHeadings = { "Home", "TShirts", "Hoodies", "Jackets",
                                         "Check-Out", "Admin", "About" };

        // View label displayed on each Tab
        string[] tabLabels = { "Home", "TShirts", "Hoodies", "Jackets",
                                       "Check-Out", "Admin", "About" };

        // View method name for each Tab
        string[] tabViews = { "Index", "Tab1Orders", "Tab2Orders", "Tab3Orders",
                                       "CheckOut", "Admin", "About" };

        // Text for View Heading of any Options columns
        string[] optionsColumnHeading = { "", "Accessory", "Discount", "Modification",
                                              "", "", "" };

        // The tax rate is 5%
        decimal taxRate = 0.05M;

        // Action Method for Home page View
        public ActionResult Index()
        {
            Session["PageHeading"] = siteHeading;

            ViewBag.Message = "Welcome To " + siteHeading;
            ViewBag.Message2 = "<img src=\"/Images/logo.jpg\">";
            ViewBag.Message2 += "<br />This is a clothing store of all time";
            return View();
        }

        // Action Method for About page View
        public ActionResult About()
        {
            Session["PageHeading"] = siteHeading;

            ViewBag.Message = "About " + siteHeading;
            ViewBag.Message2 = "Zaid Khan.";
            return View();
        }

        // Action Method for First Product View
        public ActionResult Tab1Orders()
        {
            return GetTabView(1);
        }

        // Action Method to Process First Product View
        [HttpPost]
        public ActionResult Tab1Orders(string button, FormCollection collection)
        {
            return ProcessTabView(1, button, collection);
        }

        // Action Method for second Product View
        public ActionResult Tab2Orders()
        {
            return GetTabView(2);
        }

        // Action Method to Process second Product View
        [HttpPost]
        public ActionResult Tab2Orders(string button, FormCollection collection)
        {
            return ProcessTabView(2, button, collection);
        }

        // Action Method for third Product View
        public ActionResult Tab3Orders()
        {
            return GetTabView(3);
        }

        // Action Method to Process third Product View
        [HttpPost]
        public ActionResult Tab3Orders(string button, FormCollection collection)
        {
            return ProcessTabView(3, button, collection);
        }

        

        

    }
}
