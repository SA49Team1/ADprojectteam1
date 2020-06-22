using ADprojectteam1.DB;
using ADprojectteam1.Filter;
using ADprojectteam1.Models;
using ADprojectteam1.Service;
using PagedList;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADprojectteam1.Controllers
{
    [StoreClerkFilter]
    public class StoreClerkController : Controller
    {
        // GET: StoreClerk
        public ActionResult TobeCollectList()
        {
           
            ////////////////////////////////////////////////////////Dispatch features
            ///Retrieve all reqitems need to be deal with
            List<ReqItem> lri = ReqItemData.GetAllReqItemApproved();
            List<ReqItem> slist = new List<ReqItem>();
            List<ReqItem> xlist = new List<ReqItem>();
            var itemIdset = new HashSet<int>(lri.Select(x => x.item.Id).ToList());
            var depIdset = new HashSet<int>(lri.Select(x => x.emp.department.Id).ToList());
            var empIdset = new HashSet<int>(lri.Select(x => x.emp.Id).ToList());
            List<List<List<ReqItem>>> lll = new List<List<List<ReqItem>>>();
            List<List<ReqItem>> ll = new List<List<ReqItem>>();
            Dictionary<int, Dictionary<int, int>> list = new Dictionary<int, Dictionary<int, int>>();
           

            foreach (int itemId in itemIdset)
            {
                slist = lri.Where(x=>x.item.Id==itemId).ToList();

                Dictionary<int, int> depmap = new Dictionary<int, int>();
                foreach (int depId in depIdset)
                {
                    
                    xlist = slist.Where(x => x.emp.department.Id == depId&&x.item.Id==itemId).ToList();
                     depmap.Add(depId,xlist.Select(x=>x.Quant).Sum());
                    foreach (int empId in empIdset)
                    {//ReqItemData.SetReqItemCollecting(empId, itemId);

                    }
                }
                list.Add(itemId,depmap);
               
            }
            
            ViewBag.Rlist = list;
            

            return View();
        }

        [HttpPost]
        public JsonResult ConfirmCollect(int Id, int quant)
        {
            Dictionary<int, int> collist = new Dictionary<int, int>();
            if (Session["collist"]!=null)
            collist=((Dictionary<int,int>)Session["collist"]);

            
            if (collist == null)
            {
                collist = new Dictionary<int, int>();
                collist.Add(Id,quant);
                
            }

            else 
            {
                if (collist.ContainsKey(Id)) collist[Id] = quant;
                else collist.Add(Id, quant);

            }
  
            Session["collist"] = collist;

            object new_q = new { };
            return Json(new_q, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GenerateDelOrder()
        {


            Dictionary<int, int> collist = new Dictionary<int, int>();
            if (Session["collist"] != null)
                collist = (Dictionary<int, int>)Session["collist"];
            else
                Session["collist"] = new Dictionary<int,int>();

            
            List<ReqItem> lri = ReqItemData.GetAllReqItemApproved();
            var itemIdset = new HashSet<int>(collist.Keys);
            var depIdset = new HashSet<int>(lri.Select(x => x.emp.department.Id).ToList());
            List<ReqItem> slist = new List<ReqItem>();
            List<ReqItem> xlist = new List<ReqItem>();
            Dictionary<int, Dictionary<int, int>> list = new Dictionary<int, Dictionary<int, int>>();




            foreach (int depId in depIdset)
            {
                slist = lri.Where(x => x.emp.department.Id == depId).ToList();

                Dictionary<int, int> itemmap = new Dictionary<int, int>();
                foreach (int itemId in itemIdset)
                {

                    xlist = slist.Where(x => x.item.Id==itemId).ToList();
                    int quant= xlist.Select(x => x.Quant).Sum();
                    double price = StockCardData.GetLatestPriceByItem(ItemData.GetItemById(itemId));
                    if(quant!=0) itemmap.Add(itemId,quant);
                    DepOrderData.CreateDepOrder(depId,itemId,quant,price);

                    //Withdraw from stock
                    Item item = ItemData.GetItemById(itemId);
                    int balance = StockCardData.GetStockBalanceByItem(item);
                    StockCardData.WithdrawFromStockRecord(item,DateTime.Today,DepartmentData.GetDepById(depId),quant,balance);
                    int stockbalance = StockCardData.GetStockBalanceByItem(item);
                    
                    //if (stockbalance < item.ReorderLevel) Send Notification
                }
                list.Add(depId, itemmap);

            }
            ViewBag.Rlist = list;
            Session["plannedlist"] = list;
            

           
           
            return View();
        }

        [HttpPost]
        public JsonResult ChangePlan(int itemId, int depId, int quant)
        {
            int totalitemquant = 0;
            Dictionary<int, Dictionary<int, int>> plannedlist = new Dictionary<int, Dictionary<int, int>>();
            if (Session["plannedlist"] != null)
            { plannedlist = (Dictionary<int, Dictionary<int,int>>)Session["plannedlist"]; }

            if (plannedlist.ContainsKey(depId)&&plannedlist[depId].ContainsKey(itemId)) 
            {
                plannedlist[depId][itemId] = quant;
                

                foreach (int dId in plannedlist.Keys)
                {


                    foreach(int iId in plannedlist[dId].Keys)
                    {
                        
                       if(iId==itemId) totalitemquant += plannedlist[dId][iId];

                    }
                }

            }

            

            Session["plannedlist"] = plannedlist;
            object new_amount = new { Id = itemId, quant = totalitemquant };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConfirmDelOrder()
        {
            object result;
            Dictionary<int, Dictionary<int, int>> plannedlist = new Dictionary<int, Dictionary<int, int>>();
            if (Session["plannedlist"] != null) { plannedlist = (Dictionary<int, Dictionary<int, int>>)Session["plannedlist"]; }

            //Check if the collected quant is tally with deliver plan
            Dictionary<int, int> collist = (Dictionary<int, int>)Session["collist"];
            foreach (int itemId in collist.Keys)
            {
                int itemtotal = 0;
                foreach (int depId in plannedlist.Keys)
                {
                    foreach (int iId in plannedlist[depId].Keys)
                    { 
                        if(iId==itemId) itemtotal += plannedlist[depId][itemId]; 
                    }
                }
                if (itemtotal != collist[itemId])
                {
                    result = new { status = false };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }

            //Set ItemReq status and Dep Order status
            foreach (int depId in plannedlist.Keys)
            {
                bool notifystatus = false;
                foreach (int itemId in plannedlist[depId].Keys)
                {
                    DepOrderData.SetCollected(depId, itemId, plannedlist[depId][itemId]);

                    
                    foreach (int empId in DepartmentData.GetAllEmpByDepId(depId).Select(x => x.Id))
                    {

                        if (ReqItemData.SetReqItemCollected(empId, itemId)&&notifystatus==false)
                        {
                            
                            string emailaddress = EmployeeData.FindEmpById(empId).EmailAdd;
                            Task task = Task.Run(() =>
                            {
                                EmailNotification.SendNotificationEmailToEmployee(emailaddress,"Stationary Requisition Status Changed", "Your Stationary Requisition is under delivering");
                            });
                            notifystatus = true;
                        }


                    }
                }
            }
            result = new { status=true };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CollectedDepOrder()
        {
            Dictionary<int, Dictionary<int, int>> plannedlist = new Dictionary<int, Dictionary<int, int>>();
            
            List<Department> ldep=DepartmentData.GetAllDep();
            
            //load the collected dep order
            
                foreach (int depId in ldep.Select(x => x.Id))
                {
                    List<DepOrder> listdp = DepOrderData.GetCollectedDepOrderByDepId(depId);

                Dictionary<int, int> dp = new Dictionary<int, int>();
                foreach (int itemId in listdp.Select(x => x.item.Id))
                    { 
                        dp.Add(itemId,listdp.Where(x=>x.item.Id==itemId).FirstOrDefault().collectedquant);
                        
                    }
                if (!plannedlist.ContainsKey(depId)) plannedlist.Add(depId, dp);
                else plannedlist[depId] = dp;
                }
                Session["plannedlist"] = plannedlist;
                            
            
            ViewBag.Rlist = plannedlist;
            Session.Remove("collist");
            Session.Remove("plannedlist");

            return View();

           
        }



        /////////////////////////////////////////////////////////////////////////ReOrder features
        public ActionResult TobeReOrderList()
        {
            ItemData.SetSupplier();
            List<Item> listItem = ItemData.FindAll();

            
               
            

            //Get all items which is in process of reOrder
            List<Item> listinprocess = PurchaseOrderData.GetItemsInProcess();

            //Get all items whose stockbalance is below reorderLevel
            List<Item> listreorder = listItem.Where(x=>x.ReorderLevel> StockCardData.GetStockBalanceByItem(x)).ToList();
            //Screen out items in process
            listinprocess.ForEach(x=>listreorder.Remove(x));
            
            
            Dictionary<int, int> selectedsupplier = new Dictionary<int, int>();
            foreach (Item item in listreorder)
            {
                int itId = item.Id;
                int suId = item.Supplier1.supplier.Id;
                selectedsupplier.Add(item.Id, item.Supplier1.supplier.Id);
            }
            



            Session["selectedsupplier"] = selectedsupplier;
            ViewBag.Rlist = listreorder;


            return View();
        }

        [HttpPost]
        public JsonResult SelectSupplier(int itemId, int sId)
        {
            Dictionary<int,int> selectedsupplier = new Dictionary<int, int>();
            if (Session["selectedsupplier"] != null)
            { selectedsupplier = (Dictionary<int, int>)Session["selectedsupplier"]; }
            else
            {
                throw new Exception("please log in"); 
            }

            selectedsupplier[itemId] = sId;

            Session["selectedsupplier"] = selectedsupplier;
            object new_amount = new {  };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GeneratePendingPO()
        {
            List<PurchaseOrder> listPO =new  List<PurchaseOrder>();

            Dictionary<int, int> selectedsupplier = new Dictionary<int, int>();
            if (Session["selectedsupplier"] != null)
            { selectedsupplier = (Dictionary<int, int>)Session["selectedsupplier"]; }
            else
            {
                throw new Exception("please log in");
            }


            //Group the reorder demand by selected supplier
            Dictionary<int, List<int>> itemsforeachsupplier = new Dictionary<int, List<int>>();
            
            var supplierIdset = new HashSet<int>(selectedsupplier.Values);
            foreach (int supId in supplierIdset)
            {
                List<int> itemlist = new List<int>();
                foreach (int itemId in selectedsupplier.Keys)
                {
                    if (selectedsupplier[itemId] == supId) {
                        if (itemsforeachsupplier.ContainsKey(supId)) itemsforeachsupplier[supId].Add(itemId);
                        else
                        {
                            itemlist.Add(itemId);
                            itemsforeachsupplier.Add(supId, itemlist);
                        } 
                    }
                    
                }
                
            }

            //Create PO
            foreach (int supId in itemsforeachsupplier.Keys)
            {
                
                List<ItemSupplier> lrr = new List<ItemSupplier>();

                foreach (int itemId in itemsforeachsupplier[supId])
                { ItemSupplier its=ItemSupplierData.GetByItemIdAndSupplierId(itemId,supId);
                    int itId = its.Id;
                    int suId = its.supplier.Id;
                   
                    lrr.Add(its);
                    //ReOrderRecordData.CreateReOrderRocord(its);
                }
                
                PurchaseOrderData.CreatePO(lrr);
            }



            ViewBag.reorderrecordgroupbysupplier = itemsforeachsupplier;
            Session.Remove("selectedsupplier");



            object new_amount = new { };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ManagePO()
        {
            //Get all PO in process
            List<PurchaseOrder> pendingpo = PurchaseOrderData.GetPendingPO();
            List<PurchaseOrder> sentpo = PurchaseOrderData.GetSentPO();
            List<PurchaseOrder> confirmedpo = PurchaseOrderData.GetConfirmedPO();
            Dictionary<PurchaseOrder, List<Item>> ppo = new Dictionary<PurchaseOrder, List<Item>>();
            Dictionary<PurchaseOrder, List<Item>> spo = new Dictionary<PurchaseOrder, List<Item>>();
            Dictionary<PurchaseOrder, List<Item>> cpo = new Dictionary<PurchaseOrder, List<Item>>();
            foreach (PurchaseOrder po in pendingpo)
            {
                List<Item> li = new List<Item>();
                foreach (ItemSupplier rr in po.items)
                {
                    if (ppo.ContainsKey(po)) ppo[po].Add(rr.item);
                    else
                    {
                        li.Add(rr.item);
                        ppo.Add(po, li);

                    }
                }
            }

            foreach (PurchaseOrder po in sentpo)
            {
                List<Item> li = new List<Item>();
                foreach (ItemSupplier rr in po.items)
                {
                    if (spo.ContainsKey(po)) spo[po].Add(rr.item);
                    else
                    {
                        li.Add(rr.item);
                        spo.Add(po, li);

                    }
                }
            }
            foreach (PurchaseOrder po in confirmedpo)
            {
                List<Item> li = new List<Item>();
                foreach (ItemSupplier rr in po.items)
                {
                    if (cpo.ContainsKey(po)) cpo[po].Add(rr.item);
                    else
                    {
                        li.Add(rr.item);
                        cpo.Add(po, li);

                    }
                }
            }
            ViewBag.Plist = ppo;
            ViewBag.Slist = spo;
            ViewBag.Clist = cpo;


            return View();
        }

        [HttpPost]
        public JsonResult SendPO(int pId)
        {
            //Link to send email to supplier
            PurchaseOrderData.setStatus(pId,"sent");
            object new_amount = new { };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConfirmPO(int pId)
        {
            
            PurchaseOrderData.setStatus(pId, "confirmed");
            object new_amount = new { };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RejectPO(int pId)
        {
            //Link to send email to supplier
            PurchaseOrderData.setStatus(pId, "rejected");
            object new_amount = new { };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CheckInPO(int pId)
        {
            PurchaseOrder po=PurchaseOrderData.GetPOById(pId);
            //Add record to stock card
            foreach (Item item in po.items.Select(x => x.item).ToList())
            {
                int balance = StockCardData.GetStockBalanceByItem(item);
                StockCardData.AddToStock(item, DateTime.Today,po.items.FirstOrDefault().supplier.Id, po.items.Select(x => x.UnitPrice).FirstOrDefault(), po.items.Select(x => x.item.ReorderQty).FirstOrDefault(),balance);
                
            }
            
            PurchaseOrderData.setStatus(pId, "delivered");
            object new_amount = new { };

            return Json(new_amount, JsonRequestBehavior.AllowGet);
        }

        /////////////////////////////////////////////////////////////////////////////////////Inventory Adjustment Vouchour
        ///
        
        public ActionResult InvAdjForm(string searchStr, int? page)
        {
            List<Item> Plist = new List<Item>();
            Plist = ItemData.FindAll();
            ViewBag.listItem = Plist;


            List<InventoryAdj> listInvAdj = new List<InventoryAdj>();
            listInvAdj = InventoryAdjData.FindAll();
            ViewBag.InvAdjList = listInvAdj;


            List<Item> Rlist = new List<Item>();
            bool match = false;




            if (searchStr == null)
            {
                searchStr = "";
                ViewBag.Rlist = Plist.ToPagedList(page ?? 1, 7);
            }
            else
            {
                foreach (Item Pro in Plist)
                {
                    bool fit = false;
                    if (Found(Pro.Description, searchStr).fit)
                    {
                        fit = true;
                        Pro.Description = Found(Pro.Description, searchStr).str;
                    }

                    if (fit) { match = true; Rlist.Add(Pro); }
                }
                ViewBag.Rlist = Rlist.ToPagedList(page ?? 1, 7);
            }


            ViewData["searchStr"] = searchStr;
            ViewData["match"] = match;



            return View();
        }

        public searchResult Found(string ba, string ta)
        {

            string s = ba;
            int index = ba.IndexOf(ta, StringComparison.CurrentCultureIgnoreCase);
            if (index != -1)
            {

                s = ba.Substring(0, index) + "<span class='font-red'>" + ba.Substring(index, ta.Length) + "</span>" + ba.Substring(index + ta.Length);
            }

            return new searchResult { fit = (index != -1), str = s };

        }

        [HttpPost]
        public JsonResult SubmitInvAdj(int Id, int quant,string reason)
        {
            Item it = ItemData.GetItemById(Id);
            
            InventoryAdjData.CreateInvAdj(it,quant,reason);
            object new_q = new { };
            return Json(new_q, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public JsonResult deleteInvAdj(int Id)
        {
            Item it = ItemData.GetItemById(Id);

            InventoryAdjData.DeleteInvAdj(Id);
            object new_q = new { };
            return Json(new_q, JsonRequestBehavior.AllowGet);

        }
    }


}