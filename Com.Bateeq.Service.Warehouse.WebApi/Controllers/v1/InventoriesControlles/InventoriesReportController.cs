﻿using AutoMapper;
using Com.Bateeq.Service.Warehouse.Lib.Facades;
//using Com.Bateeq.Service.Warehouse.Lib.Facades.InventoryFacades;
using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Services;
using Com.Bateeq.Service.Warehouse.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using General = Com.Bateeq.Service.Warehouse.WebApi.Helpers.General;

namespace Com.Bateeq.Service.Warehouse.WebApi.Controllers.v1.InventoryControllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/inventories/monitoring")]
    [Authorize]
    public class InventoryReportController : Controller
    {
        private string ApiVersion = "1.0.0";
        private readonly IMapper mapper;
        private readonly InventoryFacade facade;
        private readonly IdentityService identityService;

        public InventoryReportController(IMapper mapper, InventoryFacade facade, IdentityService identityService)
        {
            this.mapper = mapper;
            this.facade = facade;
            this.identityService = identityService;
        }

        #region By User
        [HttpGet("by-user")]
        //public IActionResult GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order = "{}")
        public IActionResult GetReport(string storageId, string filter, int page = 1, int size = 25, string Order = "{}")
        {
            int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
            identityService.Username = User.Claims.Single(p => p.Type.Equals("username")).Value;
            string accept = Request.Headers["Accept"];

            try
            {

                var data = facade.GetReport(storageId, filter, page, size, Order, offset, identityService.Username);

                return Ok(new
                {
                    apiVersion = ApiVersion,
                    data = data.Item1,
                    info = new { total = data.Item2 },
                    message = General.OK_MESSAGE,
                    statusCode = General.OK_STATUS_CODE
                });
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }

        [HttpGet("by-user/download")]
        public IActionResult GetXls(string storageId, string filter)
        {

            try
            {
                byte[] xlsInBytes;
                //int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : Convert.ToDateTime(dateFrom);
                //DateTime DateTo = dateTo == null ? DateTime.Now : Convert.ToDateTime(dateTo);
                string filename;

                var xls = facade.GenerateExcelReportByUser(storageId, filter);


                filename = String.Format("Repoort Monthly Stock{0}.xlsx", DateTime.UtcNow.ToString("dd-MMM-yyyy"));

                xlsInBytes = xls.ToArray();
                var file = File(xlsInBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                return file;

            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }
        #endregion

        #region By Search
        [HttpGet("by-search")]
        //public IActionResult GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order = "{}")
        public IActionResult GetReport(string itemCode, int page = 1, int size = 25, string Order = "{}")
        {
            int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
            identityService.Username = User.Claims.Single(p => p.Type.Equals("username")).Value;
            string accept = Request.Headers["Accept"];

            try
            {

                var data = facade.GetSearch(itemCode, page, size, Order, offset, identityService.Username);

                return Ok(new
                {
                    apiVersion = ApiVersion,
                    data = data.Item1,
                    info = new { total = data.Item2 },
                    message = General.OK_MESSAGE,
                    statusCode = General.OK_STATUS_CODE
                });
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }
        #endregion

        #region By Movements
        [HttpGet("by-movements")]
        //public IActionResult GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order = "{}")
        public IActionResult GetMovements(string storageId, string itemCode, string info, int page = 1, int size = 25, string Order = "{}")
        {
            int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
            identityService.Username = User.Claims.Single(p => p.Type.Equals("username")).Value;
            string accept = Request.Headers["Accept"];

            try
            {

                var data = facade.GetMovements(storageId, itemCode, info, Order, offset, identityService.Username, page, size);

                return Ok(new
                {
                    apiVersion = ApiVersion,
                    data = data.Item1,
                    info = new { total = data.Item2 },
                    message = General.OK_MESSAGE,
                    statusCode = General.OK_STATUS_CODE
                });
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }

        [HttpGet("by-movements/download")]
        public IActionResult GetMovementXls(string storageId, string itemCode)
        {

            try
            {
                byte[] xlsInBytes;
                //int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : Convert.ToDateTime(dateFrom);
                //DateTime DateTo = dateTo == null ? DateTime.Now : Convert.ToDateTime(dateTo);
                string filename;

                var xls = facade.GenerateExcelReportByMovement(storageId, itemCode);


                filename = String.Format("Report Movement Stock- {0}.xlsx", DateTime.UtcNow.ToString("dd-MMM-yyyy"));

                xlsInBytes = xls.ToArray();
                var file = File(xlsInBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                return file;

            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }
        #endregion

        #region Age Inventory
        [HttpGet("age")]
        public IActionResult GetInventoryAge(int storageId, string keyword)
        {
            try
            {
                var indexAcceptPdf = Request.Headers["Accept"].ToList().IndexOf("application/pdf");

                var viewModel = facade.GetInventoryAge(storageId, keyword);

                if (indexAcceptPdf < 0)
                {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                    .Ok(viewModel);
                return Ok(Result);
                }
                else
                {
                    byte[] xlsInBytes;
                    //int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                    //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : Convert.ToDateTime(dateFrom);
                    //DateTime DateTo = dateTo == null ? DateTime.Now : Convert.ToDateTime(dateTo);
                    string filename;

                    var xls = facade.GenerateExcelInventoryAge(storageId, keyword);

                    filename = String.Format("Report Inventory Age - {0}.xlsx", DateTime.UtcNow.ToString("dd-MMM-yyyy"));

                    xlsInBytes = xls.ToArray();
                    var file = File(xlsInBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                    return file;
                }
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }
        #endregion

        #region Stock Availability
        [HttpGet("stock-availability")]
        public IActionResult GetAllStockByStorageId(string storageId)
        {
            try
            {
                var data = facade.GetAllStockByStorageId(storageId);
                Dictionary<string, object> Result =
                                      new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                                      .Ok(data);
                return Ok(Result);
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }

        }

        [HttpGet("nearest-storage-stock")]
        public async Task<IActionResult> GetNearestStorageStock(string storageCode, string itemCode)
        {
            try
            {
                var viewModel = await facade.GetNearestStorageStock(storageCode, itemCode);

                Dictionary<string, object> Result =
                       new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                       .Ok(viewModel);
                return Ok(Result);
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }

        }
        #endregion

        #region Monthly Stock
        [HttpGet("monthly-stock")]
        public IActionResult GetOverallMonthlyStock(string month, string year)
        {
            try
            {
                var viewModel = facade.GetOverallMonthlyStock(year, month);

                Dictionary<string, object> Result =
                       new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                       .Ok(viewModel);
                return Ok(Result);
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }

        }

        [HttpGet("monthly-stock/storage")]
        public IActionResult GetOverallStorageStock(string code, string month, string year)
        {
            try
            {
                var viewModel = facade.GetOverallStorageStock(code, year, month);

                Dictionary<string, object> Result =
                       new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                       .Ok(viewModel);
                return Ok(Result);
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }

        }

        [HttpGet("monthly-stock/download")]
        public IActionResult GenerateOverallStorageStockExcel(string code, string month, string year)
        {
            try
            {
                byte[] xlsInBytes;
                //int offset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : Convert.ToDateTime(dateFrom);
                //DateTime DateTo = dateTo == null ? DateTime.Now : Convert.ToDateTime(dateTo);
                string filename;
                var xls = facade.GenerateExcelForLatestStockByStorage(code, month, year);

                filename = String.Format("Report Monthly Stock - {0} - {1}.xlsx", code , DateTime.UtcNow.ToString("MM-yyyy"));

                xlsInBytes = xls.ToArray();
                var file = File(xlsInBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                return file;

            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }

        }
        #endregion

        #region By RO

        [HttpGet("by-ro")]
        public IActionResult GetInventoryStockByRo(string ro)
        {
            try
            {
                var data = facade.GetInventoryReportByRo(ro);
                Dictionary<string, object> Result =
                                      new ResultFormatter(ApiVersion, General.OK_STATUS_CODE, General.OK_MESSAGE)
                                      .Ok(data);
                return Ok(Result);
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                    new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                    .Fail();
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }


        #endregion

    }
}