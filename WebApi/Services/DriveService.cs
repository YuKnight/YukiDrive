using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using YukiDrive.Models;
using System.Threading.Tasks;
using YukiDrive.Helpers;
using YukiDrive.Contexts;

namespace YukiDrive.Services
{
    public class DriveService : IDriveService
    {
        IDriveAccountService accountService;
        GraphServiceClient graph;
        SiteContext siteContext;
        DriveContext driveContext;
        public DriveService(IDriveAccountService accountService, SiteContext siteContext, DriveContext driveContext)
        {
            this.accountService = accountService;
            graph = accountService.Graph;
            this.siteContext = siteContext;
            this.driveContext = driveContext;
        }
        /// <summary>
        /// 获取根目录的所有项目
        /// </summary>
        /// <returns></returns>
        public async Task<List<DriveFile>> GetRootItems(string siteName = "onedrive", bool showHiddenFolders = false)
        {
            string siteId = GetSiteId(siteName);
            var drive = (siteName != "onedrive") ? graph.Sites[siteId].Drive : graph.Me.Drive;
            var result = await drive.Root.Children.Request().GetAsync();
            List<DriveFile> files = GetItems(result, siteName, showHiddenFolders);
            return files;
        }
        /// <summary>
        /// 根据id获取文件夹下所有项目
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<DriveFile>> GetDriveItemsById(string id, string siteName = "onedrive", bool showHiddenFolders = false)
        {
            string siteId = GetSiteId(siteName);
            var drive = (siteName != "onedrive") ? graph.Sites[siteId].Drive : graph.Me.Drive;
            var result = await drive.Items[id].Children.Request().GetAsync();
            List<DriveFile> files = GetItems(result, siteName, showHiddenFolders);
            return files;
        }
        /// <summary>
        /// 根据路径获取文件夹下所有项目
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<DriveFile>> GetDriveItemsByPath(string path, string siteName = "onedrive", bool showHiddenFolders = false)
        {
            string siteId = GetSiteId(siteName);
            var drive = (siteName != "onedrive") ? graph.Sites[siteId].Drive : graph.Me.Drive;
            var result = await drive.Root.ItemWithPath(path).Children.Request().GetAsync();
            List<DriveFile> files = GetItems(result, siteName, showHiddenFolders);
            return files;
        }
        /// <summary>
        /// 根据路径获取项目
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<DriveFile> GetDriveItemByPath(string path, string siteName = "onedrive")
        {
            string siteId = GetSiteId(siteName);
            var drive = (siteName != "onedrive") ? graph.Sites[siteId].Drive : graph.Me.Drive;
            var result = await drive.Root.ItemWithPath(path).Request().GetAsync();
            DriveFile file = GetItem(result);
            return file;
        }
        /// <summary>
        /// 根据id获取项目
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DriveFile> GetDriveItemById(string id, string siteName = "onedrive")
        {
            string siteId = GetSiteId(siteName);
            var drive = (siteName != "onedrive") ? graph.Sites[siteId].Drive : graph.Me.Drive;
            var result = await drive.Items[id].Request().GetAsync();
            DriveFile file = GetItem(result);
            return file;
        }
        #region PrivateMethod
        private DriveFile GetItem(DriveItem result)
        {
            DriveFile file = new DriveFile()
            {
                CreatedTime = result.CreatedDateTime,
                Name = result.Name,
                Size = result.Size,
                Id = result.Id
            };
            object downloadUrl;
            if (result.AdditionalData != null)
            {
                //可能是文件夹
                if (result.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out downloadUrl))
                    file.DownloadUrl = (string)downloadUrl;
            }

            return file;
        }

        private List<DriveFile> GetItems(IDriveItemChildrenCollectionPage result, string siteName = "onedrive", bool showHiddenFolders = false)
        {
            List<DriveFile> files = new List<DriveFile>();
            string[] hiddenFolders = siteContext.Sites.Single(site => site.Name == siteName).HiddenFolders;
            foreach (var item in result)
            {
                //要隐藏文件
                if (!showHiddenFolders)
                {
                    //跳过隐藏的文件
                    if (hiddenFolders != null)
                    {
                        if (hiddenFolders.Any(str => str == item.Name))
                        {
                            continue;
                        }
                    }
                }
                DriveFile file = new DriveFile()
                {
                    CreatedTime = item.CreatedDateTime,
                    Name = item.Name,
                    Size = item.Size,
                    Id = item.Id
                };
                object downloadUrl;
                if (item.AdditionalData != null)
                {
                    //可能是文件夹
                    if (item.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out downloadUrl))
                        file.DownloadUrl = (string)downloadUrl;
                }
                files.Add(file);
            }

            return files;
        }

        /// <summary>
        /// 根据名称返回siteid
        /// </summary>
        /// <returns></returns>
        private string GetSiteId(string siteName)
        {
            Models.Site site = siteContext.Sites.SingleOrDefault(site => site.Name == siteName);
            if (site == null)
            {
                return null;
            }
            return site.SiteId;
        }
        #endregion
    }
}