﻿using MvcWebRole1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MvcWebRole1.Controllers
{
    public class VkontakteController : Controller
    {
        //
        // GET: /Vkontakte/

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        String client_id = "5070823";
        String redirect_uri = "http://localhost:65413/Vkontakte/getToken/";
        String client_secret = "7LCpt00ZrUCThUwyyYum";
        String api_version = "5.33";
        String[] scopeParams;

        public RedirectResult Auth()
        {
            #region scopeParams
            scopeParams = new String[6];
            scopeParams[0] = "friends";
            scopeParams[1] = "groups";
            scopeParams[2] = "stats";
            scopeParams[3] = "ads";
            scopeParams[4] = "offline";
            scopeParams[5] = "wall";
            #endregion
            String scopeParamsString = "";
            for (int i = 0; i < scopeParams.Length; i++)
            {
                if (i != scopeParams.Length - 1)
                {
                    scopeParamsString += scopeParams[i] + ",";
                }
                else
                {
                    scopeParamsString += scopeParams[i];
                }
            }
            return new RedirectResult("https://oauth.vk.com/authorize?client_id=" + client_id + "&scope=" + scopeParamsString + "&response_type=code");
        }

        public RedirectResult getToken(String code)
        {
            Console.WriteLine("getTokenMethod");
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String test = "https://oauth.vk.com/access_token?client_id=" + client_id + "&client_secret=" + client_secret + "&code=" + code + "&redirect_uri=" + redirect_uri;
            String access_token = wc.DownloadString("https://oauth.vk.com/access_token?client_id=" + client_id + "&client_secret=" + client_secret + "&code=" + code);
            JObject obj = JObject.Parse(access_token);
            String VkToken = (string)obj["access_token"];

            DatabaseContext db = new DatabaseContext();

            string login = HttpContext.User.Identity.Name;
            User user = db.Users.Where(u => u.Email == login).FirstOrDefault();
            SocAccount socAcc = db.SocAccounts.Where(u => u.ID_USER == user.Id).FirstOrDefault();
            socAcc.SOCNET_TYPE = 0;
            socAcc.TOKEN = VkToken;
            if (user != null)
            {
                db.Entry(socAcc).State = EntityState.Modified;
                db.SaveChanges();
            }         
            return RedirectPermanent("~/Tool/Social/");
        }

        public String GetSocAccProfileImg(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?fields=photo_200_orig&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["photo_200_orig"];
            return jtoken.ToString();
        }

        public String GetSocAccName(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken_f = obj["response"].First["first_name"];
            JToken jtoken_s = obj["response"].First["last_name"];
            return jtoken_f.ToString() + " " + jtoken_s.ToString();
        }
    }
    public static class VKWorker
    {
        public static List<int> getGroupSubscribersIds(int groupId)
        {
             WebClient wc = new WebClient();
             wc.Encoding = Encoding.UTF8;
             String answer = wc.DownloadString("https://api.vk.com/method/groups.getMembers?group_id=" + groupId);
             JObject obj = JObject.Parse(answer);
             JToken jtoken = obj["response"]["users"].First;

             List<int> ids = new List<int>();
             do
             {
                 ids.Add((int)jtoken);
                 jtoken = jtoken.Next;
             }
             while (jtoken != null);

             return ids;
        }
        public static String GetSocAccName(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken_f = obj["response"].First["first_name"];
            JToken jtoken_s = obj["response"].First["last_name"];
            return jtoken_f.ToString() + " " + jtoken_s.ToString();
        }
        public static String getGroupName(Group gr)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/groups.getById?group_id=" + gr.ID_GROUP);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["name"];
            return jtoken.ToString();
        }
        public static List<ClientComment> getSocialCommentsFromGroup(int ID_GROUP)
        {
            DatabaseContext db = new DatabaseContext();
            List<ClientComment> ccs = db.ClientComments
                 .Join(db.ContentsInGroups,
                 c => c.ID_CIG,
                 q => q.ID_CIG,
                 (c, q) => new { c, q }).Where(w => w.q.ID_GROUP == ID_GROUP).Select(v => v.c).ToList();
            return ccs;
        }
        public static String GetSocAccProfileImg(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String answer = wc.DownloadString("https://api.vk.com/method/users.get?fields=photo_50&access_token=" + token);
            JObject obj = JObject.Parse(answer);
            JToken jtoken = obj["response"].First["photo_50"];
            return jtoken.ToString();
        }
        public static Tuple<String,String, String> getCommentData(ClientComment cc)
        {
            DatabaseContext db = new DatabaseContext();
            int ID_GROUP = db.ContentsInGroups.Join(db.ClientComments,
                c => c.ID_CIG,
                q => q.ID_CIG,
                (c, q) => new { c, q }).Select(w => w.c.ID_GROUP).First();
            String VK_ID_GR = db.Groups.Where(g=>g.ID==ID_GROUP).Select(w=>w.ID_GROUP).Single();

            int ID_POST = db.ContentsInGroups.Where(c => c.ID_CIG == cc.ID_CIG).Select(c => c.ID_POST).Single();



            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            String test = "https://api.vk.com/method/wall.getComments?owner_id=-" + VK_ID_GR + "&post_id=" + ID_POST + "&start_comment_id=" + cc.ID_COMMENT + "&count=1&v=5.37";
            String answer = wc.DownloadString("https://api.vk.com/method/wall.getComments?owner_id=-" + VK_ID_GR + "&post_id=" + ID_POST + "&start_comment_id=" + cc.ID_COMMENT + "&count=1&v=5.37");
            JObject obj = JObject.Parse(answer);
            String text = obj["response"]["items"].First["text"].ToString();

            String clientName = db.Clients.Where(c => c.ID_CL == cc.ID_CL).Select(c => c.NAME).Single();
            String client_ID_VK = db.Clients.Where(c => c.ID_CL == cc.ID_CL).Select(c => c.ID_VK).Single();
            answer = wc.DownloadString("https://api.vk.com/method/users.get?user_ids=" + client_ID_VK + "&fields=photo_50");
            obj = JObject.Parse(answer);
            String photoUrl = obj["response"].First["photo_50"].ToString();



            return new Tuple<string, string, string>(photoUrl, clientName, text);
        }
    }
}