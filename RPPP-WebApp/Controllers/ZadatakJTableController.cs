using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RPPP_WebApp.Models;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    [Route("jtable/zadatakAPI/[action]")]
    public class ZadatakJTableController : JTableController<ZadatakAPIController, int, ZadatakViewModel>
    {
        public ZadatakJTableController(ZadatakAPIController controller) : base(controller)
        {
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Update([FromForm] ZadatakViewModel model)
        {
            return await base.UpdateItem(model.IdZad, model);
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Delete([FromForm] int idZad)
        {
            return await base.DeleteItem(idZad);
        }


    }
}
