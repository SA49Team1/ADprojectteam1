﻿using ADprojectteam1.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADprojectteam1.Models
{
    public class DepOrder
    {
        public int Id { get; set; }
        public virtual Item item { get; set; }
        public virtual Department dep { get; set; }
        public int quant { get; set; }
        public double uprice { get; set; }
        public int collectedquant { get; set; }
        public int deliveredquant { get; set; }
        public string status { get; set; }//can be "acknowledged","collected","delivered"
        public DateTime signindate { get; set; }
        public ICollection<SRequisition> listreq { get; set; }

        public DepOrder()
        {
            status = "acknowledged";
        }

        public DepOrder(Item it,Department d,int q,double price,  string s, DateTime sdate)
        {
            item = it;
            dep = d;
            quant = q;
            uprice = price;
            collectedquant = q;
            deliveredquant = q;
            status = s;
            signindate = sdate;

        }

        public double GetOrderAmount()
        {
            double amount = 0;
            amount = StockCardData.FindPriceByItemId(item.Id) * quant;
            return amount;
        }

        public override bool Equals(object obj)
        {
            return obj is DepOrder order &&
                   Id == order.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }
    }
}