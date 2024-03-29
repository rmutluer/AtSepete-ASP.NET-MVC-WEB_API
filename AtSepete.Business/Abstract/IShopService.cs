﻿using AtSepete.Dtos.Dto.Shop;
using AtSepete.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtSepete.Business.Abstract
{
    public interface IShopService
    {
        Task<IDataResult<List<ShopListDto>>> ShopListAsync();
        Task<IDataResult<List<ShopProductDetailDto>>> ShopProductDetailAsync(Guid productId);
        Task<IDataResult<List<ShopFilterListDto>>> ShopFilterListAsync(string filterName);
        Task<IDataResult<List<ShopSideBarFilterListDto>>> ShopSideBarFilterListAsync(string sideBarFilter);
        Task<IDataResult<List<BestSellerProductListDto>>> BestSellerProductsAsync();
    }
}
