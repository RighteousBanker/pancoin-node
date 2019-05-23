using Microsoft.AspNetCore.Mvc;
using Panode.API;

namespace Panode.API.Controllers
{
    public class CustomControllerBase : ControllerBase
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public override OkObjectResult Ok(object value)
        {
            return base.Ok(new ViewModel(value));
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public override BadRequestObjectResult BadRequest(object error)
        {
            return base.BadRequest(new ViewModel(error));
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public BadRequestObjectResult BadRequest(string message)
        {
            return base.BadRequest(new ViewModel(message));
        }
    }
}
