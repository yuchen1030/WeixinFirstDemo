using Senparc.Weixin.MP.MessageHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Senparc.Weixin.MP.Entities;
using System.IO;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.AppStore;
using Senparc.Weixin.MP.Helpers;
using Senparc.Weixin.Entities.Request;
using Senparc.Weixin.Helpers.Extensions;

namespace WeixinFirstDemo.WXService
{
    public class CustomMessageHandler : MessageHandler<CustomMessageContext>
    {
        public CustomMessageHandler(Stream inputStream, PostModel postModel = null, int maxRecordCount = 0, DeveloperInfo developerInfo = null) : base(inputStream, postModel, maxRecordCount, developerInfo)
        {
            base.CurrentMessageContext.ExpireMinutes = 10;
        }

        //默认处理
        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = requestMessage.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "当前服务器时间：" + DateTime.Now;
            return responseMessage;
        }

        public override void OnExecuting()
        {
            var storageModel = CurrentMessageContext.StorageData as StorageModel;
            if (storageModel != null && storageModel.IsInCmd)
            {
                storageModel.CmdCount++;
                if (storageModel.CmdCount >= 20)
                {
                    var responseMessageText = RequestMessage.CreateResponseMessage<ResponseMessageText>();
                    responseMessageText.Content = "CmdCount已经大于等于2！";
                    ResponseMessage = responseMessageText;
                    base.CancelExcute = true;
                }
            }
            base.OnExecuting();
        }

        public override void OnExecuted()
        {
            if (ResponseMessage is ResponseMessageText)
            {
                (ResponseMessage as ResponseMessageText).Content += "\r\n【艾康生物AI部】";

                //数据库处理（t >5 s）
                //队列、线程/Thread
            }

            base.OnExecuted();
        }


        //文本消息
        public override IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            var handler = requestMessage.StartHandler(false)
                .Keyword("cmd", () =>
                {
                    var responseMessageText = requestMessage.CreateResponseMessage<ResponseMessageText>();
                    CurrentMessageContext.StorageData = new StorageModel()
                    {
                        IsInCmd = true
                    };
                    responseMessageText.Content += "已经进入CMD状态";
                    return responseMessageText;
                })
                .Keyword("你好", () =>
                {
                    var responseMessageText = requestMessage.CreateResponseMessage<ResponseMessageText>();
                    responseMessageText.Content += "您好，很高兴为您服务，请问有什么可以帮到您？";
                    return responseMessageText;
                })
                .Keywords(new string[] { "刚才我说了什么", "?", "？" }, () =>
               {
                   List<string> all = new List<string>();
                   for (int i = 0; i < CurrentMessageContext.RequestMessages.Count - 1; i++)//存储3分钟内的至多20条消息
                   {
                       var historyMessage = CurrentMessageContext.RequestMessages[i];
                       all.Add((historyMessage is RequestMessageText) ? (historyMessage as RequestMessageText).Content : "[非文本]" + historyMessage.MsgType);
                       all[i] = all[i] + " -- " + historyMessage.CreateTime;
                   }
                   var responseMessageText = requestMessage.CreateResponseMessage<ResponseMessageText>();
                   responseMessageText.Content += "刚才您说了以下的话：" + Environment.NewLine + string.Join(Environment.NewLine, all);
                   return responseMessageText;
               })
                .Keywords(new[] { "exit", "quit", "close" }, () =>
                {
                    var responseMessageText = requestMessage.CreateResponseMessage<ResponseMessageText>();

                    var storageModel = CurrentMessageContext.StorageData as StorageModel;
                    if (storageModel != null)
                    {
                        storageModel.IsInCmd = false;
                    }
                    return responseMessageText;
                }).Regex(@"^http", () =>
                {
                    var responseMessageNews = requestMessage.CreateResponseMessage<ResponseMessageNews>();

                    var news = new Article()
                    {
                        Title = "您输入了网址：" + requestMessage.Content,
                        Description = "这里是描述，第一行\r\n这里是描述，第二行",
                        PicUrl = "https://www.baidu.com/img/bd_logo1.png",
                        Url = requestMessage.Content
                    };
                    responseMessageNews.Articles.Add(news);
                    return responseMessageNews;
                }).Default(() =>
                {
                    var responseMessageText = requestMessage.CreateResponseMessage<ResponseMessageText>();
                    responseMessageText.Content = "这是一条默认的文本请求回复信息 @ " + DateTime.Now;
                    return responseMessageText;
                });

            var responseMessage = handler.ResponseMessage;
            if (responseMessage is ResponseMessageText)
            {
                if (string.IsNullOrEmpty((responseMessage as ResponseMessageText).Content))
                {
                    (responseMessage as ResponseMessageText).Content = "您输入了文字：" + requestMessage.Content;
                }
                var storageModel = CurrentMessageContext.StorageData as StorageModel;
                if (storageModel != null)
                {
                    (responseMessage as ResponseMessageText).Content += "当前CMD Count：" +
                    storageModel.CmdCount;
                }
            }
            return handler.ResponseMessage as IResponseMessageBase;
        }

        //位置消息
        public override IResponseMessageBase OnLocationRequest(RequestMessageLocation requestMessage)
        {
            var responseMessage = requestMessage.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "您发送的位置信息为：{0}(维度为{1}，经度为{2})".FormatWith(requestMessage.Label, requestMessage.Location_X, requestMessage.Location_Y);
            return responseMessage;
        }

        //点击事件
        public override IResponseMessageBase OnEvent_ClickRequest(RequestMessageEvent_Click requestMessage)
        {
            if (requestMessage.EventKey == "acon")
            {
                var responseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
                var news = new Article()
                {
                    Title = "您点击了按钮：" + requestMessage.EventKey,
                    Description = "这里是描述，第一行\r\n这里是描述，第二行",
                    PicUrl = "http://www.aconlabs.com.cn/Uploads/2017-07-26/597848f6415b1.jpg",
                    Url = "http://www.aconlabs.com.cn/"
                };
                responseMessage.Articles.Add(news);
                return responseMessage;
            }
            if (requestMessage.EventKey == "A")
            {
                var responseMessage = requestMessage.CreateResponseMessage<ResponseMessageText>();
                var storageModel = CurrentMessageContext.StorageData as StorageModel;
                if (storageModel != null)
                {
                    if (storageModel.IsInCmd)
                    {
                        responseMessage.Content = "当前已经进入CMD状态";
                        responseMessage.Content += "\r\n您的上一条消息类型为：" + CurrentMessageContext.RequestMessages.Last().MsgType;
                    }
                    else
                    {
                        responseMessage.Content = "当前已经退出CMD状态";
                    }
                }
                else
                {
                    responseMessage.Content = "找不到Session数据";
                }
                return responseMessage;
            }
            if (requestMessage.EventKey == "B")
            {
                return new ResponseMessageNoResponse();
            }
            else
            {
                var responseMessage = requestMessage.CreateResponseMessage<ResponseMessageText>();
                responseMessage.Content = "您点击了按钮：" + requestMessage.EventKey;
                return responseMessage;
            }
        }

    }
}