﻿using ADprojectteam1.DB;
using ADprojectteam1.Filter;
using ADprojectteam1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ADprojectteam1.Service;
using PagedList;
using PagedList.Mvc;


namespace ADprojectteam1.Controllers
{
    [DepEmpFilter]
    public class DepEmpController : Controller
    {
        // GET: DepEmp
        public ActionResult RequisitionList()
        {
            List<SRequisition> lreq = new List<SRequisition>();
            string user = (string)Session["username"];
            bool dele= EmployeeData.GetDelegateStatusByUserName(user);
            ViewBag.delestatus = dele;
            if (dele) Session["sessionRole"] = "DeleManager";

            lreq = SrequisitionData.FindAllByUsername(user);
            ViewBag.listreq = lreq;
            
            
            return View();
        }

        public ActionResult deleteReq(int id)
        {
            SrequisitionData.deleteReqById(id);

            return RedirectToAction("RequisitionList");
        }

        [HttpPost]
        public JsonResult AddReqItem(int Id,int quant)
        {
            SRequisition sr = new SRequisition();
            Employee user = EmployeeData.FindByUserName((string)Session["username"]);
            int id = user.Id;
            sr = (SRequisition)Session["reqform"];
           
            if (sr == null)
            {
                sr = new SRequisition();
                sr.ListItem = new List<ReqItem>();
                Item p = new Item();
                p = ItemData.GetItemById(Id);
                ReqItem reqitem = new ReqItem(p, user, quant);

                sr.ListItem.Add(reqitem);

            }

            else if (!sr.ListItem.Where(x => x.item.Id == Id).Any())
            {
                Item p = new Item();
                p = ItemData.GetItemById(Id);
                ReqItem reqitem = new ReqItem(p, user, quant);

                sr.ListItem.Add(reqitem);

            }
            else
            {
                ReqItem ri = new ReqItem();
                ri = sr.ListItem.Where(x => x.item.Id == Id).FirstOrDefault();
                ri.Quant = quant;
            }

           



            Session["reqform"] = sr;

            object new_q = new { quant = sr.ListItem.Sum(x => x.Quant) };
            return Json(new_q, JsonRequestBehavior.AllowGet);

        }

        public ActionResult SubmitReqForm()
        {
            SRequisition sr = (SRequisition)Session["reqform"];
            SrequisitionData.SaveReq(sr);
            Employee e = EmployeeData.FindByUserName((string)Session["username"]);
            Employee manager = EmployeeData.FindEmpById(e.department.DepHeadId);

            Task task = Task.Run(() => {
                EmailNotification.SendNotificationEmailToEmployee(manager.EmailAdd,"New Stationary Requisition Reminder","There is a new Stationary Requistion for your approval.");
            });
            
            
            
            Session.Remove("reqform");

            return RedirectToAction("RequisitionList");
        }

    

    public ActionResult RequisitionForm(string searchStr, int? page)
        {
            List<Item> Plist = new List<Item>();
            Plist = ItemData.FindAll();
            ViewBag.listItem = Plist;
            
            List<Item> Rlist = new List<Item>();
            bool match = false;
            
            if (searchStr == null)
            {
                searchStr = "";
                ViewBag.Rlist = Plist.ToPagedList(page ?? 1,7);
            }
            else
            {
                foreach (Item Pro in Plist)
                {
                    bool fit = false;
                    if (Search.Found(Pro.Description, searchStr).fit)
                    {
                        fit = true;
                        Pro.Description = Search.Found(Pro.Description, searchStr).str;
                    }
                    
                    if (fit) { match = true; Rlist.Add(Pro); }
                }
                ViewBag.Rlist = Rlist.ToPagedList(page ?? 1,7);
            }


            ViewData["searchStr"] = searchStr;
            ViewData["match"] = match;


            string user = (string)Session["username"];
            bool dele = EmployeeData.GetDelegateStatusByUserName(user);
            ViewBag.delestatus = dele;
            if (dele) Session["sessionRole"] = "DeleManager";

            return View();
        }



        
    }
}