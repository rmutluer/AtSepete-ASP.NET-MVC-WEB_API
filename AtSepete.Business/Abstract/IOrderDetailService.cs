﻿using AtSepete.Dtos.Dto.OrderDetails;
using AtSepete.Entities.Data;
using AtSepete.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtSepete.Business.Abstract
{
    public interface IOrderDetailService
    {
        Task<IDataResult<List<OrderDetailListDto>>> GetAllOrderDetailAsync();
        Task<IDataResult<List<OrderDetailListDto>>> GetAllOrderDetailWihtNameAsync();
        Task<IDataResult<List<OrderDetailListDto>>> GetAllByFilterOrderDetailAsync();
        Task<IDataResult<OrderDetailDto>> GetByIdOrderDetailAsync(Guid id);
        Task<IDataResult<OrderDetailDto>> GetOrderDetailWithNames(Guid id);
        Task<IDataResult<CreateOrderDetailDto>> AddOrderDetailAsync(CreateOrderDetailDto entity);
        Task<IDataResult<UpdateOrderDetailDto>> UpdateOrderDetailAsync(Guid id, UpdateOrderDetailDto updateOrderDetailDto);
        Task<IResult> HardDeleteOrderDetailAsync(Guid id);
        Task<IResult> SoftDeleteOrderDetailAsync(Guid id);
        
    }
}
