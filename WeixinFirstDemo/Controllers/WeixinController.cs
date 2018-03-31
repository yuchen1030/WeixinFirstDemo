using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using WeixinFirstDemo.WXService;

namespace WeixinFirstDemo.Controllers
{
    public class WeixinController : Controller
    {
        public static readonly string Token = WebConfigurationManager.AppSettings["WeixinToken"];//与微信公众账号后台的Token设置保持一致，区分大小写。

        public static readonly string EncodingAESKey = WebConfigurationManager.AppSettings["WeixinEncodingAESKey"];//与微信公众账号后台的EncodingAESKey设置保持一致，区分大小写。

        readonly Func<string> _getRandomFileName = () => DateTime.Now.ToString("yyyyMMdd-HHmmss") + Guid.NewGuid().ToString("n").Substring(0, 6);


        // GET: Weixin
        [HttpGet]
        public ActionResult Index(PostModel postModel, string echostr)
        {
            if (CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
            {
                return Content(echostr); //返回随机字符串则表示验证通过
            }
            else
            {
                return Content("failed:" + postModel.Signature + "," + CheckSignature.GetSignature(postModel.Timestamp, postModel.Nonce, Token) + "。" +
                    "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
            }
        }

        [HttpPost]
        //[ActionName("Index")]
        public ActionResult Index(PostModel postModel)
        {
            postModel.EncodingAESKey = EncodingAESKey;

            //创建MessgeHandler实例
            var messageHandler = new CustomMessageHandler(Request.InputStream, postModel, 10);

            //去重
            messageHandler.OmitRepeatedMessage = true;

            //执行
            messageHandler.Execute();

            //返回
            return new FixWeixinBugWeixinResult(messageHandler);
        }

    }
}