using Microsoft.AspNetCore.Mvc;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.ViewModels;
using System.Diagnostics;

namespace RPPP_WebApp.Controllers
{
    [Route("jtable/vrstaZadatak/[action]")]
    public class VrstaZadatakJTableController : JTableController<VrstaZadatakApiController, int, VrstaZadatakViewModel>
    {
        public VrstaZadatakJTableController(VrstaZadatakApiController controller) : base(controller)
        {
            Debug.WriteLine("----------------------------------------HERE----------------------------------------");
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Update([FromForm] VrstaZadatakViewModel model)
        {
            return await base.UpdateItem(model.IdVrstaZad, model);
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Delete([FromForm] int Id)
        {
            return await base.DeleteItem(Id);
        }
    }
}
