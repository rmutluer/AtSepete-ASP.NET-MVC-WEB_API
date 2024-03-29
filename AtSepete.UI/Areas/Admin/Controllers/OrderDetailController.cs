﻿using AtSepete.Dtos.Dto.OrderDetails;
using AtSepete.Dtos.Dto.Orders;
using AtSepete.Dtos.Dto.OrderDetails;
using AtSepete.UI.ApiResponses.OrderApiResponse;
using AtSepete.UI.ApiResponses.OrderDetailApiResponse;
using AtSepete.UI.ApiResponses.OrderDetailApiResponse;
using AtSepete.UI.Areas.Admin.Models.OrderDetailVMs;
using AtSepete.UI.Areas.Admin.Models.OrderVMs;
using AtSepete.UI.Areas.Admin.Models.OrderDetailVMs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NToastNotify;
using System.Net;
using System.Text;

namespace AtSepete.UI.Areas.Admin.Controllers
{
    public class OrderDetailController : AdminBaseController
    {
        private readonly IMapper _mapper;

        public OrderDetailController(IToastNotification toastNotification, IConfiguration configuration, IMapper mapper) : base(toastNotification, configuration)
        {
            _mapper = mapper;
        }
        public async Task<IActionResult> OrderDetailList(string customerFullName=null,string productFullName=null)
        {
            using (var httpClient = new HttpClient())
            {

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserToken);
                using (HttpResponseMessage response = await httpClient.GetAsync($"{ApiBaseUrl}/OrderDetail/GetAllOrderDetails"))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshTokenLogin", "Login", new { returnUrl = HttpContext.Request.Path, area = "" });
                    }
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    OrderDetailListResponse orderDetailList = JsonConvert.DeserializeObject<OrderDetailListResponse>(apiResponse);
                    if (orderDetailList.IsSuccess)
                    {
                        ViewBag.CustomerFullName=customerFullName;
                        ViewBag.ProductFullName=productFullName;
                        var orderDetails = _mapper.Map<List<OrderDetailListDto>, List<AdminOrderDetailListVM>>(orderDetailList.Data);
                        NotifySuccessLocalized(orderDetailList.Message);
                        return View(orderDetails);
                    }
                    else
                    {
                        NotifyErrorLocalized(orderDetailList.Message);
                        return RedirectToAction("Index", "Admin");
                    }
                };

            }
        }

        public async Task<IActionResult> OrderDetail(Guid id)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserToken);
                using (HttpResponseMessage response = await httpClient.GetAsync($"{ApiBaseUrl}/OrderDetail/GetByIdOrderDetail/{id}"))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshTokenLogin", "Login", new { returnUrl = HttpContext.Request.Path, area = "" });
                    }
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    OrderDetailResponse orderDetailResponse = JsonConvert.DeserializeObject<OrderDetailResponse>(apiResponse);
                    if (orderDetailResponse.IsSuccess)
                    {
                        var orderDetail = _mapper.Map<OrderDetailDto, AdminOrderDetailVM>(orderDetailResponse.Data);//data'ların response' den boş gelme ihtimalkeri de kontrol edilmeli
                        NotifySuccessLocalized(orderDetailResponse.Message);
                        return View(orderDetail);
                    }
                    else
                    {
                        NotifyErrorLocalized(orderDetailResponse.Message);
                        return RedirectToAction("OrderDetailList");
                    }
                };
            }
        }
        [HttpGet]
        public async Task<IActionResult> UpdateOrderDetail(Guid id)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserToken);
                using (HttpResponseMessage response = await httpClient.GetAsync($"{ApiBaseUrl}/OrderDetail/GetByIdOrderDetail/{id}"))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshTokenLogin", "Login", new { returnUrl = HttpContext.Request.Path, area = "" });
                    }
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    OrderDetailResponse orderDetail = JsonConvert.DeserializeObject<OrderDetailResponse>(apiResponse);
                    if (orderDetail.IsSuccess)
                    {
                        var updateOrderDetail = _mapper.Map<OrderDetailDto, AdminOrderDetailUpdateVM>(orderDetail.Data);
                        
                        NotifySuccessLocalized(orderDetail.Message);
                        return View(updateOrderDetail);
                    }
                    else
                    {
                        NotifyErrorLocalized(orderDetail.Message);
                        return RedirectToAction("OrderDetailList");
                    }
                };
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateOrderDetail(AdminOrderDetailUpdateVM adminOrderDetailUpdateVM)
        {
            using (var httpClient = new HttpClient())
            {
                var updateOrderDetailDto = _mapper.Map<AdminOrderDetailUpdateVM, UpdateOrderDetailDto>(adminOrderDetailUpdateVM);
               
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserToken);
                StringContent content = new StringContent(JsonConvert.SerializeObject(updateOrderDetailDto), Encoding.UTF8, "application/Json");
                using (HttpResponseMessage response = await httpClient.PutAsync($"{ApiBaseUrl}/OrderDetail/UpdateOrderDetail/{adminOrderDetailUpdateVM.Id}", content))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return RedirectToAction("RefreshTokenLogin", "Login", new { returnUrl = HttpContext.Request.Path, area = "" });
                    }
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    UpdateOrderDetailResponse updateOrderDetail = JsonConvert.DeserializeObject<UpdateOrderDetailResponse>(apiResponse);
                    if (updateOrderDetail.IsSuccess)
                    {
                        NotifySuccessLocalized(updateOrderDetail.Message);
                        return RedirectToAction("OrderDetailList");
                    }
                    else
                    {
                        NotifyErrorLocalized(updateOrderDetail.Message);                        
                        return View(adminOrderDetailUpdateVM);
                    }
                   
                };

            }
        }
    }
}
