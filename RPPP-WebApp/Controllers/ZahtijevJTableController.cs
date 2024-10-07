using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RPPP_WebApp.Models;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers
{
    /// <summary>
    /// JTable kontroler razred za zahtijev
    /// </summary>
    [Route("jtable/zahtijevApi/[action]")]
    public class ZahtijevJTableController : JTableController<ZahtijevAPIController, int, ZahtijevViewModel>
    {
        public ZahtijevJTableController(ZahtijevAPIController controller) : base(controller)
        {
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Update([FromForm] ZahtijevViewModel model)
        {
            return await base.UpdateItem(model.IdZah, model);
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Delete([FromForm] int idZah)
        {
            return await base.DeleteItem(idZah);
        }


    }
}
