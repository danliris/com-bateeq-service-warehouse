using Com.Bateeq.Service.Warehouse.Lib.Models.SOModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.SOViewModel;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.Bateeq.Service.Warehouse.Lib.Interfaces.SOInterfaces
{
    public interface ISODoc
    {
        Tuple<List<SODocs>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        SODocs ReadById(int id);
        Tuple<bool, List<object>> UploadValidate(ref List<SODocsCsvViewModel> Data, List<KeyValuePair<string, StringValues>> Body);
        List<string> CsvHeader { get; }
        Task UploadData(SODocs data, string username);
        Task<int> Process(SODocs model, string username, int clientTimeZoneOffset = 7);
        Task<SODocsViewModel> MapToViewModel(List<SODocsCsvViewModel> data, string source);
        //Task<int> Process(SODocs model, string username, int clientTimeZoneOffset = 7);
    }
}
