﻿using AtSepete.Entities.BaseData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtSepete.Entities.Data
{
    public class Market:Base
    {

        public string MarketName { get; set; }
        public string Description { get; set; }
        public string Adress { get; set; }
        public string PhoneNumber { get; set; }
        //navigation property
        virtual public IEnumerable<ProductMarket> ProductMarkets{ get; set; }
        virtual public IEnumerable<Order>? Orders { get; set; }

    }
}
