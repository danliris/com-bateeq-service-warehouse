using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces.SOInterfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.AdjustmentDocsModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.InventoryModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.SOModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.TransferModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.SOViewModel;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using CsvHelper.TypeConversion;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Bateeq.Service.Warehouse.Lib.Facades
{
    public class SOFacade : ISODoc
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<TransferInDoc> dbSetTransferIn;
        private readonly DbSet<SODocs> dbSetSO;
        private readonly IServiceProvider serviceProvider;
        private readonly DbSet<Inventory> dbSetInventory;
        private readonly DbSet<InventoryMovement> dbSetInventoryMovement;
        private readonly DbSet<TransferOutDoc> dbSetTransferOut;

        public SOFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSetSO = dbContext.Set<SODocs>();
            this.dbSetTransferIn = dbContext.Set<TransferInDoc>();
            this.dbSetInventory = dbContext.Set<Inventory>();
            this.dbSetInventoryMovement = dbContext.Set<InventoryMovement>();
            this.dbSetTransferOut = dbContext.Set<TransferOutDoc>();
        }

        public string GenerateCode(string ModuleId)
        {
            var uid = ObjectId.GenerateNewId().ToString();
            var hashids = new Hashids(uid, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");
            var now = DateTime.Now;
            var begin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var diff = (now - begin).Milliseconds;
            string code = String.Format("{0}/{1}/{2}", hashids.Encode(diff), ModuleId, DateTime.Now.ToString("MM/yyyy"));
            return code;
        }

        public List<string> CsvHeader { get; } = new List<string>()
        {
            "Barcode", "Nama Barang", "Kuantitas Stock"
        };

        public sealed class SOMap : CsvHelper.Configuration.ClassMap<SODocsCsvViewModel>
        {
            public SOMap()
            {
                Map(p => p.code).Index(0);
                Map(p => p.name).Index(1);
                Map(p => p.quantity).Index(2).TypeConverter<StringConverter>();
            }
        }

        public Tuple<bool, List<object>> UploadValidate(ref List<SODocsCsvViewModel> Data, List<KeyValuePair<string, StringValues>> Body)
        {
            List<object> ErrorList = new List<object>();
            string ErrorMessage;
            bool Valid = true;

            foreach (SODocsCsvViewModel productVM in Data)
            {
                ErrorMessage = "";

                if (string.IsNullOrWhiteSpace(productVM.code))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Barcode tidak boleh kosong, ");
                }
                else if (Data.Any(d => d != productVM && d.code.Equals(productVM.code)))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Barcode tidak boleh duplikat, ");
                }
                if (string.IsNullOrWhiteSpace(productVM.name))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Nama tidak boleh kosong, ");
                }
                else if (Data.Any(d => d != productVM && d.name.Equals(productVM.name)))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Nama tidak boleh duplikat, ");
                }
                decimal quantity = 0;
                if (string.IsNullOrWhiteSpace(productVM.quantity))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Quantity tidak boleh kosong, ");
                }
                else if (!decimal.TryParse(productVM.quantity, out quantity))
                {
                    ErrorMessage = string.Concat(ErrorMessage, "Quantity harus numerik, ");
                }
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    productVM.quantity = quantity;
                }
                else
                {
                    ErrorMessage = ErrorMessage.Remove(ErrorMessage.Length - 2);
                    var Error = new ExpandoObject() as IDictionary<string, object>;
                    Error.Add("Barcode", productVM.code);
                    Error.Add("Nama Barang", productVM.name);
                    Error.Add("Kuantitas Stock", productVM.quantity);
                    ErrorList.Add(Error);
                }
            }

            if (ErrorList.Count > 0)
            {
                Valid = false;
            }

            return Tuple.Create(Valid, ErrorList);
        }

        public async Task UploadData(SODocs data, string username)
        {            
            foreach (var i in data.Items)
            {
                EntityExtension.FlagForCreate(i, username, USER_AGENT);
            }
            EntityExtension.FlagForCreate(data, username, USER_AGENT);
            dbSetSO.Add(data);
            var result = await dbContext.SaveChangesAsync();
        }

        public async Task<SODocsViewModel> MapToViewModel(List<SODocsCsvViewModel> csv, string source)
        {
            List<SODocsItemViewModel> soDocsItems = new List<SODocsItemViewModel>();
            var storages = GetStorage(source);
            foreach (var i in csv)
            {
                var item = dbContext.Inventories.Where(x => x.ItemCode == i.code && x.StorageId == storages.Id).FirstOrDefault();
                    soDocsItems.Add(new SODocsItemViewModel
                    {
                        item = new ItemViewModel
                        {
                            articleRealizationOrder = item.ItemArticleRealizationOrder,
                            _id = item.ItemId,
                            code = i.code,
                            domesticCOGS = Convert.ToDouble(item.ItemDomesticCOGS),
                            domesticSale = Convert.ToDouble(item.ItemDomesticSale),
                            domesticWholesale = Convert.ToDouble(item.ItemDomesticWholeSale),
                            domesticRetail = Convert.ToDouble(item.ItemDomesticRetail),
                            name = i.name,
                            size = item.ItemSize,
                            uom = item.ItemUom
                        },
                        qtySO = Convert.ToDouble(i.quantity),
                        qtyBeforeSO = Convert.ToDouble(item.Quantity),
                        remark = ""
                    });
            }

            SODocsViewModel soDocs = new SODocsViewModel
            {
                code = GenerateCode("EFR-SO/INT"),
                date = DateTimeOffset.Now,
                storage = new StorageViewModel
                {
                    _id = storages.Id,
                    code = storages.Code, 
                    name = storages.Name,
                    isCentral = storages.IsCentral 
                },
                items = soDocsItems,
                isProcess = false
            };

            return soDocs;
        }

        public async Task<int> Process(SODocs model, string username, int clientTimeZoneOffset = 7)
        {
            
            int Process = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    model.IsProcess = true;
                    string inventoryMovementIn = GenerateCode("EFR-TB/SO");
                    string inventoryMovementOut = GenerateCode("EFR-KB/SO");

                    List<TransferInDocItem> transferInDocsItems = new List<TransferInDocItem>();
                    List<InventoryMovement> inventoryMovements = new List<InventoryMovement>();
                    List<TransferOutDocItem> transferOutDocsItems = new List<TransferOutDocItem>();

                    EntityExtension.FlagForUpdate(model, username, USER_AGENT);
                    foreach (var i in model.Items)
                    {
                        var inventoriesAvailable = dbContext.Inventories.Where(x => x.ItemId == i.ItemId && x.StorageId == model.StorageId).FirstOrDefault();
                        i.QtyBeforeSO = inventoriesAvailable.Quantity;

                        if (i.IsAdjusted == true)
                        {
                            EntityExtension.FlagForUpdate(i, username, USER_AGENT);
                            if (i.QtyBeforeSO < i.QtySO)
                            {
                                transferInDocsItems.Add(new TransferInDocItem
                                {
                                    ArticleRealizationOrder = i.ItemArticleRealizationOrder,
                                    DomesticCOGS = i.ItemDomesticCOGS,
                                    DomesticRetail = i.ItemDomesticRetail,
                                    DomesticSale = i.ItemDomesticSale,
                                    DomesticWholeSale = i.ItemDomesticWholeSale,
                                    ItemCode = i.ItemCode,
                                    ItemId = i.ItemId,
                                    ItemName = i.ItemName,
                                    Quantity = i.QtySO,
                                    Remark = i.Remark,
                                    Size = i.ItemSize,
                                    Uom = i.ItemUom
                                });
                                EntityExtension.FlagForCreate(i, username, USER_AGENT);

                                inventoryMovements.Add(new InventoryMovement
                                {
                                    Before = i.QtyBeforeSO,
                                    After = i.QtySO,
                                    Date = DateTimeOffset.Now,
                                    ItemArticleRealizationOrder = i.ItemArticleRealizationOrder,
                                    ItemCode = i.ItemCode,
                                    ItemDomesticCOGS = i.ItemDomesticCOGS,
                                    ItemDomesticRetail = i.ItemDomesticRetail,
                                    ItemDomesticWholeSale = i.ItemDomesticRetail,
                                    ItemDomesticSale = i.ItemDomesticSale,
                                    ItemId = i.ItemId,
                                    ItemInternationalCOGS = i.ItemInternationalCOGS,
                                    ItemInternationalRetail = i.ItemInternationalRetail,
                                    ItemInternationalSale = i.ItemInternationalSale,
                                    ItemInternationalWholeSale = i.ItemInternationalWholeSale,
                                    ItemName = i.ItemName,
                                    ItemSize = i.ItemSize,
                                    ItemUom = i.ItemUom,
                                    Quantity = i.QtySO - i.QtyBeforeSO,
                                    StorageCode = model.StorageCode,
                                    StorageId = model.StorageId,
                                    StorageName = model.StorageName,
                                    Type = "IN",
                                    Reference = inventoryMovementIn,
                                    Remark = i.Remark,
                                    StorageIsCentral = model.StorageName.Contains("GUDANG") ? true : false,
                                });
                            }

                            else
                            {
                                transferOutDocsItems.Add(new TransferOutDocItem
                                {
                                    ArticleRealizationOrder = i.ItemArticleRealizationOrder,
                                    DomesticCOGS = i.ItemDomesticCOGS,
                                    DomesticRetail = i.ItemDomesticRetail,
                                    DomesticSale = i.ItemDomesticSale,
                                    DomesticWholeSale = i.ItemDomesticWholeSale,
                                    ItemCode = i.ItemCode,
                                    ItemId = i.ItemId,
                                    ItemName = i.ItemName,
                                    Quantity = i.QtySO,
                                    Remark = i.Remark,
                                    Size = i.ItemSize,
                                    Uom = i.ItemUom
                                });
                                EntityExtension.FlagForCreate(i, username, USER_AGENT);

                                inventoryMovements.Add(new InventoryMovement
                                {
                                    Before = i.QtyBeforeSO,
                                    After = i.QtySO,
                                    Date = DateTimeOffset.Now,
                                    ItemArticleRealizationOrder = i.ItemArticleRealizationOrder,
                                    ItemCode = i.ItemCode,
                                    ItemDomesticCOGS = i.ItemDomesticCOGS,
                                    ItemDomesticRetail = i.ItemDomesticRetail,
                                    ItemDomesticWholeSale = i.ItemDomesticRetail,
                                    ItemDomesticSale = i.ItemDomesticSale,
                                    ItemId = i.ItemId,
                                    ItemInternationalCOGS = i.ItemInternationalCOGS,
                                    ItemInternationalRetail = i.ItemInternationalRetail,
                                    ItemInternationalSale = i.ItemInternationalSale,
                                    ItemInternationalWholeSale = i.ItemInternationalWholeSale,
                                    ItemName = i.ItemName,
                                    ItemSize = i.ItemSize,
                                    ItemUom = i.ItemUom,
                                    Quantity = i.QtyBeforeSO - i.QtySO,
                                    StorageCode = model.StorageCode,
                                    StorageId = model.StorageId,
                                    StorageName = model.StorageName,
                                    Type = "OUT",
                                    Reference = inventoryMovementOut,
                                    Remark = i.Remark,
                                    StorageIsCentral = model.StorageName.Contains("GUDANG") ? true : false,
                                });
                            }
                        }

                        dbSetSO.Add(model);
                        Process = await dbContext.SaveChangesAsync();
                        transaction.Commit();

                    }
                }

                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }

                return Process;
            }
        }

        private StorageViewModel2 GetStorage(string code)
        {
            string itemUri = "master/storages/code";
            string queryUri = "?code=" + code;
            string uri = itemUri + queryUri;
            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
            var response = httpClient.GetAsync($"{APIEndpoint.Core}{uri}").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                StorageViewModel2 viewModel = JsonConvert.DeserializeObject<StorageViewModel2>(result.GetValueOrDefault("data").ToString());
                return viewModel;//.Where(x => x.dataDestination[0].name == name && x.dataDestination[0].code == code).FirstOrDefault();
                //throw new Exception(string.Format("{0}, {1}, {2}", response.StatusCode, response.Content, APIEndpoint.Purchasing));
            }
            else
            {
                return null;
            }
        }

        public Tuple<List<SODocs>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<SODocs> Query = this.dbSetSO.Include(m => m.Items).Where(m => m.IsDeleted == false);

            List<string> searchAttributes = new List<string>()
            {
                "Code"
            };

            Query = QueryHelper<SODocs>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<SODocs>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<SODocs>.ConfigureOrder(Query, OrderDictionary);

            Pageable<SODocs> pageable = new Pageable<SODocs>(Query, Page - 1, Size);
            List<SODocs> Data = pageable.Data.ToList();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }

        //public Tuple<List<TransferStockViewModel>, int, Dictionary<string, string>> ReadModel(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        //{
        //    var Query = from a in dbContext.TransferOutDocs
        //               join b in dbContext.SPKDocs on a.Code equals b.Reference
        //               where a.Code.Contains("EFR-KB/RTT") && b.DestinationName != "GUDANG TRANSFER STOCK"
        //               select new TransferStockViewModel
        //               {
        //                   id = (int)a.Id,
        //                   code = a.Code,
        //                   createdBy = a.CreatedBy,
        //                   createdDate = a.CreatedUtc,
        //                   destinationname = a.DestinationName,
        //                   destinationcode = a.DestinationCode,
        //                   sourcename = a.SourceName,
        //                   sourcecode = a.SourceCode,
        //                   password = b.Password,
        //                   referensi = a.Reference,
        //                   transfername = b.SourceName,
        //                   transfercode = b.SourceCode
        //               };

        //    Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
        //    //Query = QueryHelper<TransferOutDoc>.ConfigureOrder(Query, OrderDictionary);
        //
        //    Pageable<TransferStockViewModel> pageable = new Pageable<TransferStockViewModel>(Query, Page - 1, Size);
        //    List<TransferStockViewModel> Data = pageable.Data.ToList();
        //    int TotalData = pageable.TotalCount;
        //
        //    return Tuple.Create(Data, TotalData, OrderDictionary);
        //}
        public SODocs ReadById(int id)
        {
            var model = dbSetSO.Where(m => m.Id == id)
                .Include(m => m.Items)
                .FirstOrDefault();
            return model;
        }
    }
}
