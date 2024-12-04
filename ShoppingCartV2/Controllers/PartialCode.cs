using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using ShoppingCartV2.Models;

namespace ShoppingCartV2.Controllers
{
    public partial class HomeController : Controller
    {
        // Gets the list of tabs and the Site heading label
        public ActionResult GetTabs(string id)
        {
            string headStr = "";
            if (Session["PageHeading"] != null)
            {
                headStr += "<ul id=\"headmenu\"><li>";
                if (tabViews.Length > 0)
                    headStr += "<a href=\"/Home/" + tabViews[0] + "\">";
                headStr += Session["PageHeading"].ToString();
                headStr = headStr.Replace(":", "");
                if (tabViews.Length > 0)
                    headStr += "</a>";
                headStr += "</li></ul>:";
            }
            int tabNum = -1;
            for (int i = 0; i < tabViews.Length && tabNum < 0; ++i)
                if (tabViews[i].ToLower() == id.ToLower())
                    tabNum = i;
            string tabStr = "<ul id=\"tabmenu\">" + Environment.NewLine;
            for (int i = 0; i < tabViews.Length; ++i)
            {
                tabStr += "<li>";
                if (i != tabNum)
                    tabStr += "<a href=\"/Home/" + tabViews[i] + "\">";
                tabStr += tabLabels[i];
                if (i != tabNum)
                    tabStr += "</a>";
                tabStr += "</li>" + Environment.NewLine;
            }
            tabStr += "</ul>";

            return Content(headStr + tabStr);
        }

        // Gets the view and table data for the specified view tab
        public ActionResult GetTabView(int tabNum)
        {
            Session["PageHeading"] = orderHeading;
            Session["ProductTab"] = tabViews[tabNum];
            ViewBag.Message = Session["Message"] = tabHeadings[tabNum] + " Orders:";
            return View(LoadViewTableData(Session["ProductTab"].ToString(), tabNum));
        }

        // Processes the user action on a specified view tab
        public ActionResult ProcessTabView(int tabNum, string button, FormCollection collection)
        {
            string pType = Session["ProductTab"].ToString();
            int amount = Int32.Parse(Session[pType + "ItemAmount"].ToString());

            LoadSubmission(pType, amount, collection);

            for (int i = 1; i <= amount; i++)
            {
                int value;
                if (!Int32.TryParse(collection[i - 1], out value) || value < 0)
                {
                    ViewBag.Message = "<div style=\"color:#800\">" +
                                       "Error: Invalid entry in Item #" + i +
                                        "</div><br />" + Session["Message"].ToString();
                    return View(pType, LoadViewTableData(pType, tabNum));
                }
            }

            if (button == "Save And Go To Checkout")
            {
                return RedirectToAction("CheckOut");
            }
            else
            {
                // This is the View for the next product page
                return RedirectToAction(tabViews[tabNum + 1]);
            }
        }

        // Loads the submission details into session variables for each tab
        public void LoadSubmission(string name, int amount, FormCollection collection)
        {
            int off1 = 0, off2 = -1, off3 = 0, off4 = -3, value;
            string baseStr = collection["BasePrices"], nextPrice;
            string opStr = collection["OptionInfo"], nextOption, nextOption2;
            for (int i = 1; i <= amount; i++)
            {
                off1 = off2 + 1;
                off2 = baseStr.IndexOf(':', off1);
                nextPrice = baseStr.Substring(off1, off2 - off1);

                off3 = off4 + 3;
                off4 = opStr.IndexOf("###", off3);
                nextOption = opStr.Substring(off3, off4 - off3).Replace("^^^", ")<br />(");
                nextOption2 = opStr.Substring(off3, off4 - off3).Replace("^^^", ")%%% &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;    (");
                if (nextOption == "")
                {
                    nextOption = "<br />";
                    nextOption2 = "";
                }
                else
                {
                    nextOption = "<br />" + Environment.NewLine + "</b><i>(" + nextOption + ")</i><b><br />";
                    nextOption2 = "%%% &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;    (" + nextOption2 + ")";
                }

                if (!Int32.TryParse(collection[i - 1], out value))
                    continue;
                Session[String.Format("{0}Amount{1}", name, i)] = collection[i - 1];
                Session[String.Format("{0}Price{1}", name, i)] = Double.Parse(nextPrice) * value;
                Session[String.Format("{0}Option_{1}", name, i)] = nextOption;
                Session[String.Format("{0}Option2_{1}", name, i)] = nextOption2;
            }
        }

        // Converts table entries for a particular product type into a list of products for the website
        private IList<ProductWithOption> LoadViewTableData(string pType, int viewIndex)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                IList<ProductWithOption> productList = new List<ProductWithOption>();
                ProductWithOption pItem;

                ViewBag.BasePrices = "";
                ViewBag.OptionInfo = "";
                ViewBag.InitScripts = "";
                var productItems = from wp in db1.Products where (wp.ProductTab == pType) select wp;
                int index = 1;
                int listCount = 0;
                string optheading;
                foreach (var p in productItems)
                {
                    string pathlabel = "";
                    Session[pType + "ProductID" + index] = p.ProductID;
                    Session[pType + "ProductName" + index] = "";
                    if (String.IsNullOrWhiteSpace(p.ProductName) && String.IsNullOrWhiteSpace(p.ImageFile))
                        pathlabel = "[No Image]";
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(p.ProductName))
                        {
                            pathlabel = p.ProductName.Trim();
                            Session[pType + "ProductName" + index] = pathlabel;
                            if (!String.IsNullOrWhiteSpace(p.ImageFile))
                                pathlabel += ":<br />";
                        }
                        if (!String.IsNullOrWhiteSpace(p.ImageFile))
                            pathlabel += "<img src=\"/Images/" + p.ImageFile.Trim() + "\" width=\"250\" alt=\"Store Product Image\" />";
                    }
                    Session[pType + "Path" + index] = pathlabel;
                    Session[pType + "UnitPrice" + index] = p.UnitPrice;
                    ViewBag.BasePrices += p.UnitPrice + ":";
                    ViewBag.OptionInfo += "###";
                    if (Session[pType + "Amount" + index] == null)
                        ViewData["DefaultChoice" + index] = (p.DefaultAmount >= 0) ? p.DefaultAmount : 0;
                    else
                        ViewData["DefaultChoice" + index] = Int32.Parse(Session[pType + "Amount" + index].ToString());
                    ++index;

                    pItem = new ProductWithOption();
                    pItem.ProductID = p.ProductID;
                    pItem.ProductTab = p.ProductTab;
                    pItem.ProductName = p.ProductName;
                    pItem.ImageFile = p.ImageFile;
                    pItem.UnitPrice = p.UnitPrice;
                    pItem.MaxAmount = p.MaxAmount;
                    pItem.DefaultAmount = p.DefaultAmount;

                    List<string> keylist = new List<string>();
                    SortedList optionslist = new SortedList();
                    using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
                    {
                        var prodMatches = from x in db3.Intersections where (x.ProductID == p.ProductID) select x;
                        foreach (var m in prodMatches)
                        {
                            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
                            {
                                var optMatches = from y in db2.Options where (y.OptionID == m.OptionID) select y;
                                foreach (var n in optMatches)
                                {
                                    if (!keylist.Contains(n.OptionType))
                                    {
                                        keylist.Add(n.OptionType);
                                        List<string> typeList = new List<string>();
                                        if (n.OptionCost < 0)
                                            typeList.Add(n.OptionName + ": - $" + (-n.OptionCost) + "###" + n.OptionCost);
                                        else
                                            typeList.Add(n.OptionName + ": + $" + n.OptionCost + "###" + n.OptionCost);
                                        optionslist.Add(n.OptionType, typeList);
                                    }
                                    else
                                    {
                                        List<string> typeList = (List<string>)optionslist.GetByIndex(
                                                                         optionslist.IndexOfKey(n.OptionType));
                                        if (n.OptionCost < 0)
                                            typeList.Add(n.OptionName + ": - $" + (-n.OptionCost) + "###" + n.OptionCost);
                                        else
                                            typeList.Add(n.OptionName + ": + $" + n.OptionCost + "###" + n.OptionCost);
                                    }
                                }
                            }
                        }
                    }
                    optheading = optionsColumnHeading[viewIndex];
                    optheading += (!string.IsNullOrWhiteSpace(optheading) && optheading.ToLower()[optheading.Length - 1] != 's') ? "s" : "";
                    pItem.ColumnLabel = optheading;
                    pItem.TableString = "";

                    List<string> pickedlist = GetPickedOptions(pType, index - 1);

                    int count1 = 0;
                    int count3 = 1;
                    foreach (var key in keylist)
                    {
                        List<string> typeList = (List<string>)optionslist.GetByIndex(optionslist.IndexOfKey(key));
                        int count2 = 0;
                        foreach (var value in typeList)
                        {
                            if (count1 == 0)
                            {
                                pItem.TableString += "<table class=\"bdrless\" >" + Environment.NewLine;
                            }
                            if (count2 == 0)
                            {
                                pItem.TableString += "<tr><td align=\"right\"><b>" + key + "</b>: </td><td>";
                                pItem.TableString += "<select id=\"chg_" + (index - 1) + "_" + count3 +
                                                    "\" onchange=\"changePrice(" + (index - 1) + "," +
                                                    "this.value," + p.UnitPrice + ")\">" +
                                                    "<option value=\"\">-- Click To Select --</option>" +
                                                    Environment.NewLine;
                            }
                            int n = value.ToString().IndexOf("###");
                            string val1 = value.ToString().Substring(0, n);
                            string val2 = value.ToString().Substring(n + 3);
                            string selectedstr = "";
                            int pos = 0;
                            foreach (var pickedval in pickedlist)
                            {
                                if (pickedval.ToString() == val1)
                                {
                                    selectedstr = " selected";
                                    ViewBag.InitScripts += "<script type=\"text/javascript\">" + Environment.NewLine;
                                    ViewBag.InitScripts += "changePrice(" + (index - 1) + "," + val2 +
                                                             "," + p.UnitPrice + ");" + Environment.NewLine;
                                    ViewBag.InitScripts += "</script>" + Environment.NewLine;
                                    break;
                                }
                                ++pos;
                            }
                            if (selectedstr != "")
                                pickedlist.RemoveAt(pos);
                            pItem.TableString += "<option" + selectedstr + " value=\"" + val2 + "\">" +
                                                    val1 + "</option>" + Environment.NewLine;
                            ++listCount;
                            ++count1;
                            ++count2;
                        }
                        ++count3;
                        if (count2 > 0)
                            pItem.TableString += "</select></td></tr>" + Environment.NewLine;
                    }
                    if (count1 > 0)
                        pItem.TableString += "</table>" + Environment.NewLine;
                    productList.Add(pItem);
                }
                Session[pType + "ItemAmount"] = productItems.Count();

                if (listCount == 0)
                    foreach (var product in productList)
                    {
                        product.ColumnLabel = "";
                        product.TableString = "";
                    }

                return productList.ToList();
            }
        }

        // Builds a string that is an HTML lists of all of the products purchased
        private string GetSessionItemString(string name, string title, int amount, ref decimal total)
        {
            string ItemStr = "", OptionStr;
            decimal dnum;
            double dblnum;
            for (int i = 1; i <= amount; i++)
                if (!string.IsNullOrEmpty((string)Session[String.Format("{0}Amount{1}", name, i)]) &&
                              (string)Session[String.Format("{0}Amount{1}", name, i)] != "0")
                {
                    ItemStr += "<tr><td align=\"center\"><b>" +
                                      String.Format("{0} {1}", Session[name + "Amount" + i], title);
                    if ((string)Session[String.Format("{0}Amount{1}", name, i)] != "1")
                        ItemStr += (!string.IsNullOrWhiteSpace(ItemStr) && ItemStr.ToLower()[ItemStr.Length - 1] != 's') ? "s" : "";

                    if (Session[String.Format("{0}ProductName{1}", name, i)] != null &&
                           !string.IsNullOrEmpty((string)Session[String.Format("{0}ProductName{1}", name, i)]))
                        ItemStr += String.Format(":<br /><span style=\"color: #8A2BE2;\" color=\"#8A2BE2\"><i>{0}</i></span>",
                                                 (string)Session[String.Format("{0}ProductName{1}", name, i)]);
                    else
                        ItemStr += String.Format("<br />(Item {0})", i);

                    OptionStr = (string)Session[String.Format("{0}Option_{1}", name, i)];
                    ItemStr += String.Format("{0}Total: ", OptionStr);
                    dblnum = (double)Session[String.Format("{0}Price{1}", name, i)];
                    dblnum = (dblnum < 0) ? 0 : dblnum;
                    ItemStr += String.Format("{0:C}", dblnum);
                    ItemStr += "</b></td>" + Environment.NewLine;
                    ItemStr += "<td align=\"center\"><b><i>" + Session[String.Format("{0}Path{1}", name, i)];
                    ItemStr += "</i></b></td></tr>" + Environment.NewLine;
                    dnum = Convert.ToDecimal(Session[String.Format("{0}Price{1}", name, i)]);
                    dnum = (dnum < 0) ? 0 : dnum;
                    total += dnum;
                }
            return ItemStr;
        }

        // Builds a string that is an text lists of all of the products purchased
        public string GetSessionItemString2(string name, string title, int amount, ref decimal total, string prevStr)
        {
            string ItemStr = "", OptionStr;
            decimal dnum;
            for (int i = 1; i <= amount; i++)
                if (!string.IsNullOrEmpty((string)Session[String.Format("{0}Amount{1}", name, i)]) &&
                     (string)Session[String.Format("{0}Amount{1}", name, i)] != "0")
                {
                    if (!string.IsNullOrEmpty(ItemStr))
                        ItemStr += "%%%";
                    ItemStr += String.Format("{0} {1}", Session[String.Format("{0}Amount{1}", name, i)], title);
                    if ((string)Session[String.Format("{0}Amount{1}", name, i)] != "1")
                        ItemStr += (!string.IsNullOrWhiteSpace(ItemStr) && ItemStr.ToLower()[ItemStr.Length - 1] != 's') ? "s" : "";
                    if (Session[String.Format("{0}ProductName{1}", name, i)] != null &&
                           !string.IsNullOrEmpty((string)Session[String.Format("{0}ProductName{1}", name, i)]))
                        ItemStr += String.Format(" ({0}) for ",
                                                 (string)Session[String.Format("{0}ProductName{1}", name, i)]);
                    else
                        //ItemStr += String.Format(" (Option {0}) for ", i);
                        ItemStr += String.Format(" (Item {0}) for ", i);
                    dnum = Convert.ToDecimal(Session[String.Format("{0}Price{1}", name, i)]);
                    dnum = (dnum < 0) ? 0 : dnum;
                    ItemStr += String.Format("{0:C}", dnum);
                    total += dnum;

                    OptionStr = (string)Session[String.Format("{0}Option2_{1}", name, i)];
                    if (OptionStr != "")
                        ItemStr += OptionStr;
                }
            if (!string.IsNullOrEmpty(ItemStr) & !string.IsNullOrEmpty(prevStr))
                ItemStr = "%%%" + ItemStr;
            return ItemStr;
        }

        // Method for getting an List of the options picked by the user
        private List<string> GetPickedOptions(string pType, int index)
        {
            List<string> pickedlist = new List<string>();
            if (Session[pType + "Option_" + index] != null)
            {
                string optstr = Session[pType + "Option_" + index].ToString();
                for (int i = 0, j = 0; i < optstr.Length; ++i)
                {
                    if ((i = optstr.IndexOf(">(", i)) == -1)
                        break;
                    if ((j = optstr.IndexOf(")<", i)) == -1)
                        j = optstr.Length;
                    pickedlist.Add(optstr.Substring(i + 2, j - i - 2));
                    i = j;
                }
            }
            return pickedlist;
        }

        // Method for getting a product Name from a ProductID
        private string GetProductName(int productID)
        {
            string name = "";
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                var result = from p in db1.Products where (p.ProductID == productID) select p;
                if (result.Count() == 0)
                    return "[No Name]";
                Product product = result.FirstOrDefault();
                for (int i = 1; i < tabViews.Length; ++i)
                    if (tabViews[i] == product.ProductTab)
                    {
                        name = tabHeadings[i].Trim();
                        break;
                    }
                if (!string.IsNullOrEmpty(product.ProductName))
                {
                    if (name.Length > 0)
                        name += ": ";
                    return (name + product.ProductName.Trim());
                }
                result = from p in db1.Products where (p.ProductTab == product.ProductTab) select p;
                int cnt = 0;
                foreach (Product p in result)
                {
                    if (p.ProductTab == product.ProductTab)
                        break;
                    ++cnt;
                }
                if (cnt < result.Count())
                    return (name + " (Item " + (cnt + 1) + ")");
                return name;
            }
        }

        // Action Method for Checkout View
        public ActionResult CheckOut()
        {
            ViewBag.Message = "Checking Out - The following items are in your Shopping Cart:";

            string ItemList = "";
            decimal SubTotalPrice = 0.0M;
            for (int i = 0; i < tabViews.Length; ++i)
                if (Session[tabViews[i] + "ItemAmount"] != null)
                {
                    int amount = Int32.Parse(Session[tabViews[i] + "ItemAmount"].ToString());
                    ItemList += GetSessionItemString(tabViews[i], tabHeadings[i], amount, ref SubTotalPrice);
                }

            ViewBag.HasItems = false;
            if (string.IsNullOrEmpty(ItemList))
                ItemList += "<pre style=\"font-size: 16px;\"><b>  [ Nothing Was Purchased ]</b></pre>";
            else
            {
                ItemList = "<table>" + ItemList + "</table>";
                ItemList += "<p></p><style type=\"text/css\">";
                ItemList += "table.summary { border-width: 0px; }";
                ItemList += "table.summary td { border-width: 0px; font-size: 16px; ";
                ItemList += "padding: 3px; font-weight:bold; text-align:right; }";
                ItemList += "</style><table class=\"summary\">";
                ItemList += "<tr><td>SubTotal:</td><td width=\"30\"></td><td>";
                ItemList += String.Format("{0:0.00}", SubTotalPrice);
                ItemList += "</td></tr><tr><td>Tax:</td><td></td><td>";
                ItemList += String.Format("{0:0.00}", decimal.Round(SubTotalPrice * taxRate, 2));
                ItemList += "</td></tr><tr><td>Total Purchase Price:</td><td></td><td>";
                ItemList += String.Format("{0:C}", SubTotalPrice + decimal.Round(SubTotalPrice * taxRate, 2));
                ItemList += "</td></tr>";
                ItemList += "</table>";
                ViewBag.HasItems = true;
            }
            ViewBag.Message2 = ItemList;

            return View();
        }

        // Action Method for getting Customers billing (registration) information
        public ActionResult ShoppingCartAccount()
        {
            ViewBag.Message = "Please Fill In Your Billing Information";
            ViewBag.Message += " (entries with * are required):";
            return View();
        }

        // Action Method for getting Customers billing (registration) information
        [HttpPost]
        public ActionResult ShoppingCartAccount(Customer accountToCreate)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Message = "Please correct the following error(s):";
                    return View();
                }

                string ItemList = "";
                decimal SubTotalPrice = 0.0M;
                for (int i = 0; i < tabViews.Length; ++i)
                    if (Session[tabViews[i] + "ItemAmount"] != null)
                    {
                        int amount = Int32.Parse(Session[tabViews[i] + "ItemAmount"].ToString());
                        ItemList += GetSessionItemString2(tabViews[i], tabHeadings[i], amount, ref SubTotalPrice, ItemList);
                    }
                ItemList += String.Format("%%%%%%<h3>SubTotal: {0:C}", SubTotalPrice);
                ItemList += String.Format("%%%Tax: {0:C}", decimal.Round(SubTotalPrice * taxRate, 2));
                ItemList += String.Format("%%%Total Purchase Price: {0:C}</h3>", SubTotalPrice +
                                                           decimal.Round(SubTotalPrice * taxRate, 2));

                Session["Name"] = String.Format("{0} {1}", accountToCreate.FirstName, accountToCreate.LastName);
                Session["Email"] = accountToCreate.Email;
                Session["ItemList"] = ItemList;

                using (ShoppingCartDBEntities db = new ShoppingCartDBEntities())
                {
                    db.Customers.Add(accountToCreate);
                    db.SaveChanges();

                    using (ShoppingCartDBEntities4 db4 = new ShoppingCartDBEntities4())
                    {
                        PurchaseOrder order = new PurchaseOrder();
                        order.CustomerID = accountToCreate.ID;
                        order.Date = DateTime.Now;
                        order.SubTotal = SubTotalPrice;
                        order.Tax = decimal.Round(SubTotalPrice * taxRate, 2);
                        order.Total = order.SubTotal + order.Tax;

                        db4.PurchaseOrders.Add(order);
                        db4.SaveChanges();

                        int linenum = 0;
                        for (int i = 0; i < tabViews.Length; ++i)
                            if (Session[tabViews[i] + "ItemAmount"] != null)
                            {
                                int amount = Int32.Parse(Session[tabViews[i] + "ItemAmount"].ToString());
                                for (int j = 1; j <= amount; j++)
                                {
                                    string quantityStr = (string)Session[tabViews[i] + "Amount" + j];
                                    if (!string.IsNullOrEmpty(quantityStr) && quantityStr != "0" &&
                                                 Session[tabViews[i] + "ProductID" + j] != null &&
                                                        Session[tabViews[i] + "Price" + j] != null)
                                        using (ShoppingCartDBEntities5 db5 = new ShoppingCartDBEntities5())
                                        {
                                            int pid = Int32.Parse(Session[tabViews[i] + "ProductID" + j].ToString());
                                            decimal price = Decimal.Parse(Session[tabViews[i] + "Price" + j].ToString());

                                            LineItem item = new LineItem();
                                            item.OrderID = order.OrderID;
                                            item.LineNumber = ++linenum;
                                            item.ProductID = pid;
                                            item.Quantity = Int32.Parse(quantityStr);
                                            item.ExtendedPrice = price;

                                            db5.LineItems.Add(item);
                                            db5.SaveChanges();

                                            using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
                                            {
                                                List<string> pickedlist = GetPickedOptions(tabViews[i], j);
                                                var ilist = from o in db3.Intersections where (o.ProductID == pid) select o;

                                                foreach (var pickedval in pickedlist)
                                                    foreach (var ival in ilist)
                                                    {
                                                        string optionStr = "";
                                                        using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
                                                        {
                                                            Option opt = db2.Options.First(o => o.OptionID == ival.OptionID);
                                                            if (opt.OptionCost < 0)
                                                                optionStr = opt.OptionName + ": - $" + (-opt.OptionCost);
                                                            else
                                                                optionStr = opt.OptionName + ": + $" + (opt.OptionCost);
                                                        }
                                                        if (pickedval.ToString() == optionStr)
                                                        {
                                                            using (ShoppingCartDBEntities6 db6 = new ShoppingCartDBEntities6())
                                                            {
                                                                OptionAssociation oa = new OptionAssociation();
                                                                oa.OrderID = order.OrderID;
                                                                oa.ProductID = pid;
                                                                oa.OptionID = ival.OptionID;

                                                                db6.OptionAssociations.Add(oa);
                                                                db6.SaveChanges();
                                                            }
                                                            break;
                                                        }
                                                    }
                                            }
                                        }
                                }
                            }
                    }
                }

                for (int i = 0; i < tabViews.Length; ++i)
                    if (Session[tabViews[i] + "ItemAmount"] != null)
                    {
                        int amount = Int32.Parse(Session[tabViews[i] + "ItemAmount"].ToString());
                        for (int j = 1; j <= amount; ++j)
                        {
                            Session[tabViews[i] + "Amount" + j] = "0";
                            Session[tabViews[i] + "Price" + j] = 0.0;
                            Session[tabViews[i] + "Option_" + j] = "";
                            Session[tabViews[i] + "Option2_" + j] = "";
                        }
                    }

                return RedirectToAction("ConfirmPurchase");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error processing data: " + GetError(ex);
                return View();
            }
        }

        // Action Method for confirming the Customer's purchases
        public ActionResult ConfirmPurchase()
        {
            ViewBag.Message = "Thanks for making your purchase with " + siteHeading + "!";
            ViewBag.Message2 = "Your items are being shipped, and an email has been sent<br />";
            ViewBag.Message2 += "with the following order details for your records:<p></p><hr /><p></p>";
            ViewBag.Message2 += Session["ItemList"].ToString().Replace("%%%", "<br />");
            ViewBag.Message2 += "<p></p><hr />";

            return View();
        }


        // Action Method for providing admin functions
        [Authorize]
        public ActionResult Admin()
        {
            ViewBag.Message = "Website Administration:";
            return View();
        }

        // Admin view for listing all Customer Accounts
        [Authorize]
        public ActionResult Customers(string sortOrder)
        {
            using (ShoppingCartDBEntities db = new ShoppingCartDBEntities())
            {
                ViewBag.Message = "Customer Accounts:";

                ViewBag.LastNameSortParm = String.IsNullOrEmpty(sortOrder) ? "LastName desc" : "";
                ViewBag.FirstNameSortParm = (sortOrder == "FirstName" ? "FirstName desc" : "FirstName");
                ViewBag.IDSortParm = (sortOrder == "ID" ? "ID desc" : "ID");
                ViewBag.AmountSortParm = (sortOrder == "Amount" ? "Amount desc" : "Amount");

                var accounts = from a in db.Customers select a;

                List<CustomerWithPurchase> customerList = new List<CustomerWithPurchase>();
                foreach (var acc in accounts)
                {
                    using (ShoppingCartDBEntities4 db4 = new ShoppingCartDBEntities4())
                    {
                        CustomerWithPurchase customer = new CustomerWithPurchase();
                        try
                        {
                            PurchaseOrder order = db4.PurchaseOrders.First(o => o.CustomerID == acc.ID);
                            customer.TotalPrice = order.Total;
                            customer.OrderID = order.OrderID;
                        }
                        catch (Exception)
                        {
                            customer.TotalPrice = 0M;
                            customer.OrderID = -1;
                        }
                        customer.ID = acc.ID;
                        customer.FirstName = acc.FirstName;
                        customer.LastName = acc.LastName;
                        customerList.Add(customer);
                    }
                }

                switch (sortOrder)
                {
                    case "FirstName":
                        customerList = customerList.OrderBy(s => s.FirstName).ToList();
                        break;
                    case "FirstName desc":
                        customerList = customerList.OrderByDescending(s => s.FirstName).ToList();
                        break;
                    case "ID":
                        customerList = customerList.OrderBy(s => s.ID).ToList();
                        break;
                    case "ID desc":
                        customerList = customerList.OrderByDescending(s => s.ID).ToList();
                        break;
                    case "Amount":
                        customerList = customerList.OrderBy(s => s.ID).ToList();
                        break;
                    case "Amount desc":
                        customerList = customerList.OrderByDescending(s => s.ID).ToList();
                        break;
                    case "LastName desc":
                        customerList = customerList.OrderByDescending(s => s.LastName).ToList();
                        break;
                    default:
                        customerList = customerList.OrderBy(s => s.LastName).ToList();
                        break;
                }

                return View(customerList.ToList());
            }
        }

        // Admin view for reviewing a Customer Account
        [Authorize]
        public ActionResult CustomerDetails(int ID)
        {
            using (ShoppingCartDBEntities db = new ShoppingCartDBEntities())
            {
                ViewBag.Message = "Customer Account Details:";
                try
                {
                    Customer theAccount = db.Customers.First(p => p.ID == ID);
                    return View(theAccount);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for editing a Customer Account
        [Authorize]
        public ActionResult EditCustomer(int ID)
        {
            using (ShoppingCartDBEntities db = new ShoppingCartDBEntities())
            {
                ViewBag.Message = "Edit Customer Account:";
                try
                {
                    Customer theAccount = db.Customers.First(p => p.ID == ID);
                    return View(theAccount);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for editing a Customer Account
        [HttpPost, Authorize]
        public ActionResult EditCustomer(Customer account)
        {
            using (ShoppingCartDBEntities db = new ShoppingCartDBEntities())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error editing account:";
                        return View("EditCustomer", account);
                    }

                    Customer theAccount = db.Customers.First(p => p.ID == account.ID);

                    theAccount.FirstName = account.FirstName;
                    theAccount.LastName = account.LastName;
                    theAccount.StreetAddress = account.StreetAddress;
                    theAccount.City = account.City;
                    theAccount.State = account.State;
                    theAccount.Zip = account.Zip;
                    theAccount.Phone = account.Phone;
                    theAccount.Email = account.Email;
                    db.SaveChanges();

                    return RedirectToAction("Customers");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error editing account: " + GetError(ex);
                    return View("EditCustomer", account);
                }
            }
        }

        // Action view for listing all Purchase Orders
        [Authorize]
        public ActionResult Purchases(string sortOrder)
        {
            using (ShoppingCartDBEntities4 db4 = new ShoppingCartDBEntities4())
            {
                ViewBag.Message = "Purchase Orders:";

                ViewBag.OrderIDSortParm = String.IsNullOrEmpty(sortOrder) ? "OrderID desc" : "";
                ViewBag.CustomerIDSortParm = (sortOrder == "CustomerID" ? "CustomerID desc" : "CustomerID");
                ViewBag.DateSortParm = (sortOrder == "DateID" ? "DateID desc" : "DateID");
                ViewBag.TotalSortParm = (sortOrder == "Total" ? "Total desc" : "Total");

                var orders = from p in db4.PurchaseOrders select p;

                switch (sortOrder)
                {
                    case "CustomerID":
                        orders = orders.OrderBy(s => s.CustomerID);
                        break;
                    case "CustomerID desc":
                        orders = orders.OrderByDescending(s => s.CustomerID);
                        break;
                    case "DateID":
                        orders = orders.OrderBy(s => s.Date);
                        break;
                    case "DateID desc":
                        orders = orders.OrderByDescending(s => s.Date);
                        break;
                    case "Total":
                        orders = orders.OrderBy(s => s.Total);
                        break;
                    case "Total desc":
                        orders = orders.OrderByDescending(s => s.Total);
                        break;
                    case "OrderID desc":
                        orders = orders.OrderByDescending(s => s.OrderID);
                        break;
                    default:
                        orders = orders.OrderBy(s => s.OrderID);
                        break;
                }

                return View(orders.ToList());
            }
        }

        // Admin view for reviewing a Purchase Order
        [Authorize]
        public ActionResult OrderDetails(int ID)
        {
            using (ShoppingCartDBEntities4 db4 = new ShoppingCartDBEntities4())
            {
                ViewBag.Message = "Purchase Order Details:";
                try
                {
                    PurchaseOrder order = db4.PurchaseOrders.First(p => p.OrderID == ID);

                    PurchaseOrderWithDetails theOrder = new PurchaseOrderWithDetails();
                    theOrder.OrderID = order.OrderID;
                    theOrder.CustomerID = order.CustomerID;
                    theOrder.Date = order.Date;
                    theOrder.SubTotal = order.SubTotal;
                    theOrder.Tax = order.Tax;
                    theOrder.Total = order.Total;
                    theOrder.Details = "";

                    using (ShoppingCartDBEntities5 db5 = new ShoppingCartDBEntities5())
                    {
                        String details = "";
                        for (int lineNum = 1; ; lineNum++)
                        {
                            var result = from x in db5.LineItems
                                         where (x.OrderID == ID && x.LineNumber == lineNum)
                                         select x;
                            if (result.Count() == 0)
                                break;
                            LineItem item = result.FirstOrDefault();

                            string optDetails = "";
                            using (ShoppingCartDBEntities6 db6 = new ShoppingCartDBEntities6())
                            {
                                var result2 = from x in db6.OptionAssociations
                                              where (x.OrderID == ID && x.ProductID == item.ProductID)
                                              select x;
                                string optstrs = "";
                                foreach (var oa in result2)
                                {
                                    using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
                                    {
                                        Option opt = db2.Options.First(o => o.OptionID == oa.OptionID);
                                        if (opt.OptionCost < 0)
                                            optstrs = opt.OptionName + ", - $" + (-opt.OptionCost);
                                        else
                                            optstrs = opt.OptionName + ", $" + (opt.OptionCost);
                                        optDetails += "<br /><i>(Option: " + optstrs + ")</i>";
                                    }
                                }
                            }

                            details += "<tr><td align=\"center\">" + lineNum + "</td>" +
                                       "<td align=\"center\">" +
                                       "<a href=\"/Home/ProductDetails/" + item.ProductID + "\">" +
                                       GetProductName(item.ProductID) + "</a>" + optDetails + "</td>" +
                                       "<td align=\"center\">" + item.Quantity + "</td>" +
                                       "<td align=\"center\">" + ((item.ExtendedPrice < 0) ? "0.00" : string.Format("{0:N2}", item.ExtendedPrice)) + "</td></tr>";
                        }
                        if (details.Trim().Length > 0)
                        {
                            details = "<p></p><div class=\"display-label\">Line Items:</div><p></p><table><tr>" +
                                "<td align=\"center\">" +
                                "<span style=\"color: #0000FF;\" color=\"#0000FF\"><b>Line</b></span></td>" +
                                "<td align=\"center\">" +
                                "<span style=\"color: #0000FF;\" color=\"#0000FF\"><b>Name and Options</b></span></td>" +
                                "<td align=\"center\">" +
                                "<span style=\"color: #0000FF;\" color=\"#0000FF\"><b>Quantity</b></span></td>" +
                                "<td align=\"center\">" +
                                "<span style=\"color: #0000FF;\" color=\"#0000FF\"><b>Extended Price</b></span></td></tr>" +
                                             details + "</table>";
                        }
                        theOrder.Details = details;
                    }

                    return View(theOrder);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for listing all Products
        [Authorize]
        public ActionResult Products(string sortOrder)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                ViewBag.Message = "Products Table:";

                ViewBag.IDSortParm = String.IsNullOrEmpty(sortOrder) ? "ID desc" : "";
                ViewBag.TabSortParm = (sortOrder == "Tab" ? "Tab desc" : "Tab");
                ViewBag.NameSortParm = (sortOrder == "Name" ? "Name desc" : "Name");
                ViewBag.ImageSortParm = (sortOrder == "Image" ? "Image desc" : "Image");
                ViewBag.PriceSortParm = (sortOrder == "Price" ? "Price desc" : "Price");

                var productls = from p in db1.Products select p;

                switch (sortOrder)
                {
                    case "Tab":
                        productls = productls.OrderBy(p => p.ProductTab);
                        break;
                    case "Tab desc":
                        productls = productls.OrderByDescending(p => p.ProductTab);
                        break;
                    case "Name":
                        productls = productls.OrderBy(p => p.ProductName);
                        break;
                    case "Name desc":
                        productls = productls.OrderByDescending(p => p.ProductName);
                        break;
                    case "Image":
                        productls = productls.OrderBy(p => p.ImageFile);
                        break;
                    case "Image desc":
                        productls = productls.OrderByDescending(p => p.ImageFile);
                        break;
                    case "Price":
                        productls = productls.OrderBy(p => p.UnitPrice);
                        break;
                    case "Price desc":
                        productls = productls.OrderByDescending(p => p.UnitPrice);
                        break;
                    case "ID desc":
                        productls = productls.OrderByDescending(p => p.ProductID);
                        break;
                    default:
                        productls = productls.OrderBy(p => p.ProductID);
                        break;
                }
                return View(productls.ToList());

            }
        }

        // Admin view for reviewing a Product
        [Authorize]
        public ActionResult ProductDetails(int ID)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                ViewBag.Message = "Product Details:";
                try
                {
                    Product product = db1.Products.First(p => p.ProductID == ID);
                    return View(product);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for creating a Product
        [Authorize]
        public ActionResult CreateProduct()
        {
            ViewBag.Message = "Fill in the details for this Product:";
            return View();
        }

        // Admin view for creating a Product
        [HttpPost, Authorize]
        public ActionResult CreateProduct(Product product, HttpPostedFileBase FileUpload)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error creating product:";
                        return View("CreateProduct", product);
                    }

                    bool gotError = false;
                    string message = GetFileUpload(FileUpload, ref gotError);
                    if (gotError)
                    {
                        ViewBag.Message = "Upload Error: " + message;
                        return View("CreateProduct", product);
                    }

                    db1.Products.Add(product);
                    db1.SaveChanges();

                    return RedirectToAction("Products");
                }
                catch
                {
                    ViewBag.Message = "Error creating Product:";
                    return View("CreateProduct", product);
                }
            }
        }

        // Admin view for editing a Product
        [Authorize]
        public ActionResult EditProduct(int ID)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                ViewBag.Message = "Product:";
                try
                {
                    Product product = db1.Products.First(p => p.ProductID == ID);
                    return View(product);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for editing a Product
        [HttpPost, Authorize]
        public ActionResult EditProduct(Product product, HttpPostedFileBase FileUpload)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error editing product:";
                        return View("EditProduct", product);
                    }

                    Boolean gotError = false;
                    String message = GetFileUpload(FileUpload, ref gotError);
                    if (gotError)
                    {
                        ViewBag.Message = "Upload Error: " + message;
                        return View("EditProduct", product);
                    }

                    Product theProduct = db1.Products.First(p => p.ProductID == product.ProductID);

                    theProduct.ProductTab = product.ProductTab;
                    theProduct.ProductName = product.ProductName;
                    theProduct.ImageFile = product.ImageFile;
                    theProduct.UnitPrice = product.UnitPrice;
                    theProduct.MaxAmount = product.MaxAmount;
                    theProduct.DefaultAmount = product.DefaultAmount;
                    db1.SaveChanges();

                    return RedirectToAction("Products");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error editing product: " + GetError(ex);
                    return View("EditProduct", product);
                }
            }
        }

        // Admin view for deleting a Product
        [Authorize]
        public ActionResult DeleteProduct(int ID)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                ViewBag.Message = "Are You Sure You Want To Delete The Following Product?";
                try
                {
                    Product product = db1.Products.First(p => p.ProductID == ID);
                    return View(product);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for deleting a Product
        [HttpPost, Authorize]
        public ActionResult DeleteProduct(Product product)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                try
                {
                    if (product == null)
                        return RedirectToAction("Error");

                    Product theProduct = db1.Products.First(p => p.ProductID == product.ProductID);
                    db1.Products.Remove(theProduct);
                    db1.SaveChanges();

                    return RedirectToAction("Products");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error Deleting Product: " + GetError(ex);
                    return View("DeleteProduct", product);
                }
            }
        }

        // Admin view for listing all Options
        [Authorize]
        public ActionResult Options(string sortOrder)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                ViewBag.Message = "Options Table:";

                ViewBag.IDSortParm = String.IsNullOrEmpty(sortOrder) ? "ID desc" : "";
                ViewBag.TypeSortParm = (sortOrder == "Type" ? "Type desc" : "Type");
                ViewBag.NameSortParm = (sortOrder == "Name" ? "Name desc" : "Name");
                ViewBag.PriceSortParm = (sortOrder == "Price" ? "Price desc" : "Price");

                var optionls = from o in db2.Options select o;

                switch (sortOrder)
                {
                    case "Type":
                        optionls = optionls.OrderBy(o => o.OptionType);
                        break;
                    case "Type desc":
                        optionls = optionls.OrderByDescending(o => o.OptionType);
                        break;
                    case "Name":
                        optionls = optionls.OrderBy(o => o.OptionName);
                        break;
                    case "Name desc":
                        optionls = optionls.OrderByDescending(o => o.OptionName);
                        break;
                    case "Price":
                        optionls = optionls.OrderBy(o => o.OptionCost);
                        break;
                    case "Price desc":
                        optionls = optionls.OrderByDescending(o => o.OptionCost);
                        break;
                    case "ID desc":
                        optionls = optionls.OrderByDescending(o => o.OptionID);
                        break;
                    default:
                        optionls = optionls.OrderBy(o => o.OptionID);
                        break;
                }
                return View(optionls.ToList());
            }
        }

        // Admin view for reviewing an Option
        [Authorize]
        public ActionResult OptionDetails(int ID)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                ViewBag.Message = "Option Details:";
                try
                {
                    Option option = db2.Options.First(o => o.OptionID == ID);
                    return View(option);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for editing an Option
        [Authorize]
        public ActionResult EditOption(int ID)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                ViewBag.Message = "Option:";
                try
                {

                    Option option = db2.Options.First(o => o.OptionID == ID);
                    return View(option);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for editing an Option
        [HttpPost, Authorize]
        public ActionResult EditOption(Option option)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Option:";
                        return View("EditOption", option);
                    }

                    Option theOption = db2.Options.First(o => o.OptionID == option.OptionID);

                    theOption.OptionType = option.OptionType;
                    theOption.OptionName = option.OptionName;
                    theOption.OptionCost = option.OptionCost;
                    db2.SaveChanges();

                    return RedirectToAction("Options");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error Editing Option: " + GetError(ex);
                    return View("EditOption", option);
                }
            }
        }

        // Admin view for deleting a Option
        [Authorize]
        public ActionResult DeleteOption(int ID)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                ViewBag.Message = "Are You Sure You Want To Delete The Following Option?";
                try
                {
                    Option option = db2.Options.First(o => o.OptionID == ID);
                    return View(option);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for deleting a Option
        [HttpPost, Authorize]
        public ActionResult DeleteOption(Option option)
        {
            using (ShoppingCartDBEntities2 db2 = new ShoppingCartDBEntities2())
            {
                try
                {
                    if (option == null)
                        return RedirectToAction("Error");

                    Option theOption = db2.Options.First(o => o.OptionID == option.OptionID);
                    db2.Options.Remove(theOption);
                    db2.SaveChanges();

                    return RedirectToAction("Options");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error Deleting Option: " + GetError(ex);
                    return View("DeleteOption", option);
                }
            }
        }

        // Admin view for listing all Intersections
        [Authorize]
        public ActionResult Intersections(string sortOrder)
        {
            using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
            {
                ViewBag.Message = "Intersections Table:";

                ViewBag.PIDSortParm = String.IsNullOrEmpty(sortOrder) ? "PID desc" : "";
                ViewBag.OIDSortParm = (sortOrder == "OID" ? "OID desc" : "OID");

                var interls = from x in db3.Intersections select x;

                switch (sortOrder)
                {
                    case "OID":
                        interls = interls.OrderBy(x => x.OptionID);
                        break;
                    case "OID desc":
                        interls = interls.OrderByDescending(x => x.OptionID);
                        break;
                    case "PID desc":
                        interls = interls.OrderByDescending(x => x.ProductID);
                        break;
                    default:
                        interls = interls.OrderBy(x => x.ProductID);
                        break;
                }
                return View(interls.ToList());
            }
        }

        // Admin view for reviewing an Intersection
        [Authorize]
        public ActionResult IntersectionDetails(int pid, int oid)
        {
            using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
            {
                ViewBag.Message = "Intersection Details:";
                try
                {
                    Intersection inter = db3.Intersections.First(x => x.ProductID == pid && x.OptionID == oid);
                    return View(inter);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for deleting a Intersection
        [Authorize]
        public ActionResult DeleteIntersection(int pid, int oid)
        {
            using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
            {
                ViewBag.Message = "Are You Sure You Want To Delete The Following Intersection?";
                try
                {
                    Intersection inter = db3.Intersections.First(x => x.ProductID == pid && x.OptionID == oid);
                    return View(inter);
                }
                catch
                {
                    return RedirectToAction("Error");
                }
            }
        }

        // Admin view for deleting a Intersection
        [HttpPost, Authorize]
        public ActionResult DeleteIntersection(Intersection inter)
        {
            using (ShoppingCartDBEntities3 db3 = new ShoppingCartDBEntities3())
            {
                try
                {
                    if (inter == null)
                        return RedirectToAction("Error");

                    Intersection theInter = db3.Intersections.First(x => x.ProductID == inter.ProductID &&
                                                                      x.OptionID == inter.OptionID);
                    db3.Intersections.Remove(theInter);
                    db3.SaveChanges();

                    return RedirectToAction("Intersections");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error Deleting Intersection: " + GetError(ex);
                    return View("DeleteIntersection", inter);
                }
            }
        }

        // General error message view
        public ActionResult Error()
        {
            ViewBag.Message = "System Error: Invalid Data";
            return View();
        }

        // Gets the appropriate error message from an exception
        public string GetError(Exception ex)
        {
            if (ex == null || ex.Message == null)
                return "";
            if (ex.Message.ToLower().IndexOf("inner exception") > -1)
            {
                string message = ex.InnerException.ToString();
                int n = -1;
                var match = Regex.Match(message, "\\.\\s");
                if (match.Success)
                {
                    n = match.Index;
                    match = Regex.Match(message.Substring(n + 1), "\\.\\s");
                    if (match.Success)
                    {
                        n += match.Index + 1;
                        match = Regex.Match(message.Substring(n + 1), "\\.\\s");
                        if (match.Success)
                        {
                            n += match.Index + 1;
                        }
                    }
                }
                return (n < 1) ? message : message.Substring(0, n + 1);
            }
            return ex.Message;
        }

        // Method for uploading an image file and returns the filepath or an error string
        public string GetFileUpload(HttpPostedFileBase FileUpload, ref Boolean gotError)
        {
            gotError = false;
            string filePath = "";
            if (FileUpload != null && FileUpload.ContentLength > 0)
            {
                string fileName = Path.GetFileName(FileUpload.FileName);
                filePath = Path.Combine(Server.MapPath("/Images/"), fileName);
                try
                {
                    FileUpload.SaveAs(filePath);

                    Bitmap img = new Bitmap(filePath);
                }
                catch (ArgumentException)
                {
                    gotError = true;
                    return "The file uploaded was not an image file";
                }
                catch (Exception err)
                {
                    gotError = true;
                    return GetError(err);
                }
            }
            else if (FileUpload != null && FileUpload.ContentLength == 0)
            {
                gotError = true;
                return "The image file was an empty file";
            }
            return filePath;
        }
    }
}