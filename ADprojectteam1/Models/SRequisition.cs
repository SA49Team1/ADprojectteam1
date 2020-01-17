﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADprojectteam1.Models
{
    public class SRequisition
    {
        public int Id { get; set; }

        public string RFormNum { get; set; }

        public virtual ICollection<ReqItem> ListItem { get; set; }

        public string status { get; set; }//Can be "Pending, Approved/Rejected, Fulfiled(when all ReqItems' status is delivered)."

        public string remark { get; set; }

        public SRequisition() {
            status = "Pending";
        }
    }
}