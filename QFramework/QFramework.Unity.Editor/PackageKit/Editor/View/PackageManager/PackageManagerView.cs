/****************************************************************************
 * Copyright (c) 2021.1 ~ 3 liangxie
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ****************************************************************************/

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MG.MDV;
using UnityEditor;
using UnityEngine;

namespace QFramework
{
    [DisplayName("PackageKit 插件管理")]
    [PackageKitGroup("QFramework")]
    [PackageKitRenderOrder(1)]
    public class PackageManagerView : IPackageKitView, IController
    {
        public IQFrameworkContainer Container { get; set; }

        private IToolbar mCategoriesSelectorView = null;

        public List<string> Categories
        {
            get { return null; }
            set
            {
                mCategoriesSelectorView.Menus(value);
                Container.Resolve<PackageKitWindow>().Repaint();
            }
        }

        DisposableList mDisposableList = new DisposableList();

        private PackageKitWindow mPackageKitWindow;

        private IMGUILayout mLeftLayout = null;
        private Rect mLeftRect;

        private IMGUILayout mRightLayout = null;
        private Rect mRightRect;

        public void Init(IQFrameworkContainer container)
        {
            Container = container;
            var localPackageVersionModel = this.GetModel<ILocalPackageVersionModel>();

            // 左侧
            mLeftLayout = EasyIMGUI.Vertical()
                .AddChild(EasyIMGUI.Area().WithRectGetter(() => mLeftRect)
                    // 间距 20
                    .AddChild(EasyIMGUI.Vertical()
                        .AddChild(EasyIMGUI.Space().Pixel(20)))
                    // 搜索
                    .AddChild(EasyIMGUI.Horizontal()
                        .AddChild(EasyIMGUI.Label().Text("搜索:")
                            .FontBold()
                            .FontSize(12)
                            .Width(40)
                        ).AddChild(EasyIMGUI.TextField()
                            .Height(20)
                            .Self(search =>
                            {
                                search.Content
                                    .Bind(key => { this.SendCommand(new SearchCommand(key)); })
                                    .AddToDisposeList(mDisposableList);
                            })
                        )
                    )
                    .AddChild(EasyIMGUI.Scroll()
                        .AddChild(EasyIMGUI.Custom().OnGUI(() =>
                        {
                            PackageManagerState.PackageRepositories.Value
                                .OrderByDescending(p =>
                                {
                                    var installedVersion = localPackageVersionModel.GetByName(p.name);

                                    if (installedVersion == null)
                                    {
                                        return -1;
                                    }
                                    else if (installedVersion.VersionNumber < p.VersionNumber)
                                    {
                                        return 2;
                                    }
                                    else if (installedVersion.VersionNumber == p.VersionNumber)
                                    {
                                        return 1;
                                    }
                                    else
                                    {
                                        return 0;
                                    }
                                })
                                .ThenBy(p => p.name)
                                .ForEach(p =>
                                {
                                    GUILayout.BeginVertical("box");

                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label(p.name);
                                        GUILayout.FlexibleSpace();
                                    }
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    {
                                        EasyIMGUI.Box().Text(p.latestVersion)
                                            .Self(self => self.BackgroundColor = Color.green)
                                            .DrawGUI();

                                        GUILayout.FlexibleSpace();

                                        var installedVersion = localPackageVersionModel.GetByName(p.name);

                                        if (installedVersion == null)
                                        {
                                            if (GUILayout.Button(LocaleText.Import))
                                            {
                                                RenderEndCommandExecuter.PushCommand(() =>
                                                {
                                                    this.SendCommand(new ImportPackageCommand(p));
                                                });
                                            }
                                        }
                                        else if (installedVersion.VersionNumber < p.VersionNumber)
                                        {
                                            if (GUILayout.Button(LocaleText.Update))
                                            {
                                                RenderEndCommandExecuter.PushCommand(() =>
                                                {
                                                    this.SendCommand(new UpdatePackageCommand(p));
                                                });
                                            }
                                        }
                                        else if (installedVersion.VersionNumber == p.VersionNumber)
                                        {
                                            if (GUILayout.Button(LocaleText.Reimport))
                                            {
                                                RenderEndCommandExecuter.PushCommand(() =>
                                                {
                                                    this.SendCommand(new ReimportPackageCommand(p));
                                                });
                                            }
                                        }
                                    }
                                    GUILayout.EndHorizontal();

                                    GUILayout.EndVertical();

                                    var rect = GUILayoutUtility.GetLastRect();

                                    if (mSelectedPackageRepository == p)
                                    {
                                        GUI.Box(rect, "", mSelectionRect);
                                    }

                                    if (rect.Contains(Event.current.mousePosition) &&
                                        Event.current.type == EventType.MouseUp)
                                    {
                                        mSelectedPackageRepository = p;
                                        Event.current.Use();
                                    }
                                });
                        }))
                    )
                );

            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>(
                "Assets/QFramework/Framework/Plugins/Editor/Markdown/Skin/MarkdownViewerSkin.guiskin");


            var markdownViewer = new MarkdownViewer(skin, string.Empty, "");
            // 右侧
            mRightLayout = EasyIMGUI.Vertical()
                .AddChild(EasyIMGUI.Area().WithRectGetter(() => mRightRect)
                    // 间距 20
                    .AddChild(EasyIMGUI.Vertical()
                        .AddChild(EasyIMGUI.Space().Pixel(20))
                    )
                    // 详细信息
                    .AddChild(EasyIMGUI.Vertical()
                        .WithVisibleCondition(() => mSelectedPackageRepository != null)
                        // 名字
                        .AddChild(EasyIMGUI.Label()
                            .TextGetter(() => mSelectedPackageRepository.name)
                            .FontSize(30)
                            .FontBold())
                        .AddChild(EasyIMGUI.Space())
                        // 服务器版本
                        .AddChild(EasyIMGUI.Label()
                            .TextGetter(() => "服务器版本: " + mSelectedPackageRepository.latestVersion)
                            .FontSize(15))
                        // 本地版本
                        .AddChild(EasyIMGUI.Label()
                            .WithVisibleCondition(() =>
                                localPackageVersionModel.GetByName(mSelectedPackageRepository.name).IsNotNull())
                            .TextGetter(() =>
                                "本地版本:" + localPackageVersionModel.GetByName(mSelectedPackageRepository.name).Version)
                            .FontSize(15))
                        // 作者
                        .AddChild(EasyIMGUI.Label()
                            .TextGetter(() => "作者:" + mSelectedPackageRepository.author)
                            .FontSize(15))
                        // 主页
                        .AddChild(
                            EasyIMGUI.Horizontal()
                                .AddChild(EasyIMGUI.Label()
                                    .FontSize(15)
                                    .Text("插件主页:"))
                                .AddChild(EasyIMGUI.Button()
                                    .TextGetter(() => UrlHelper.PackageUrl(mSelectedPackageRepository))
                                    .FontSize(15)
                                    .OnClick(() =>
                                    {
                                        this.SendCommand(new OpenDetailCommand(mSelectedPackageRepository));
                                    })
                                )
                                .AddChild(EasyIMGUI.FlexibleSpace())
                        )
                        // 描述
                        .AddChild(EasyIMGUI.Label()
                            .TextGetter(() => "描述:")
                            .FontSize(15)
                        )
                        .AddChild(EasyIMGUI.Space())
                        // 描述内容
                        .AddChild(EasyIMGUI.Custom().OnGUI(() =>
                        {
                            markdownViewer.UpdateText(mSelectedPackageRepository.description);
                            markdownViewer.Draw();
                        }))
                    )
                );

            mPackageKitWindow = EditorWindow.GetWindow<PackageKitWindow>();

            this.SendCommand<PackageManagerInitCommand>();


            //  EasyIMGUI.Label().Text(LocaleText.FrameworkPackages).FontSize(12).Parent(mRootLayout);
            //  var verticalLayout = new VerticalLayout("box").Parent(mRootLayout);

            // EasyIMGUI.Toolbar()
            //     .Menus(new List<string>()
            //         {"all", PackageAccessRight.Public.ToString(), PackageAccessRight.Private.ToString()})
            //     .Parent(verticalLayout)
            //     .Self(self =>
            //     {
            //         self.IndexProperty.Bind(value =>
            //         {
            //             PackageManagerState.AccessRightIndex.Value = value;
            //             mControllerNode.SendCommand(new SearchCommand(PackageManagerState.SearchKey.Value));
            //         }).AddToDisposeList(mDisposableList);
            //     });
            //
            // mCategoriesSelectorView = EasyIMGUI.Toolbar()
            //     .Parent(verticalLayout)
            //     .Self(self =>
            //     {
            //         self.IndexProperty.Bind(value =>
            //         {
            //             PackageManagerState.CategoryIndex.Value = value;
            //             mControllerNode.SendCommand(new SearchCommand(PackageManagerState.SearchKey.Value));
            //         }).AddToDisposeList(mDisposableList);
            //     });

            // var packageList = new VerticalLayout("box")
            //    .Parent(verticalLayout);

            //mRepositoryList = EasyIMGUI.Scroll()
            //    .Height(mPackageKitWindow.position.height - 100)
            //    .Parent(packageList);

            // PackageManagerState.Categories.Bind(value => { Categories = value; }).AddTo(mDisposableList);
            //
            //PackageManagerState.PackageRepositories
            //  .Bind(OnRefreshList).AddToDisposeList(mDisposableList);

            // 创建双屏
            mSplitView = mSplitView = new VerticalSplitView
            {
                Split = 240,
                fistPan = rect =>
                {
                    mLeftRect = rect;
                    mLeftLayout.DrawGUI();
                },
                secondPan = rect =>
                {
                    mRightRect = rect;
                    mRightLayout.DrawGUI();
                }
            };
        }

        private static GUIStyle mSelectionRect = "SelectionRect";


        private PackageRepository mSelectedPackageRepository;


        private VerticalSplitView mSplitView;

        public void OnUpdate()
        {
        }

        public void OnGUI()
        {
            var r = GUILayoutUtility.GetLastRect();
            mSplitView.OnGUI(new Rect(new Vector2(0, r.yMax),
                new Vector2(mPackageKitWindow.position.width, mPackageKitWindow.position.height - r.height)));
        }

        public void OnDispose()
        {
            mPackageKitWindow = null;
            mSplitView = null;

            mDisposableList.Dispose();
            mDisposableList = null;

            mCategoriesSelectorView = null;

            mLeftLayout.Dispose();
            mLeftLayout = null;

            mRightLayout.Dispose();
            mRightLayout = null;
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }


        class LocaleText
        {
            public static string FrameworkPackages
            {
                get { return Language.IsChinese ? "框架模块" : "Framework Packages"; }
            }

            public static string VersionCheck
            {
                get { return Language.IsChinese ? "版本检测" : "Version Check"; }
            }

            public static string Action
            {
                get { return "..."; }
            }

            public static string Import
            {
                get { return Language.IsChinese ? "导入" : "Import"; }
            }

            public static string Update
            {
                get { return Language.IsChinese ? "更新" : "Update"; }
            }

            public static string Reimport
            {
                get { return Language.IsChinese ? "再次导入" : "Reimport"; }
            }

            public static string Uninstall
            {
                get { return Language.IsChinese ? "未安装" : "Uninstall"; }
            }

            public static string Installed
            {
                get { return Language.IsChinese ? "已安装" : "Installed"; }
            }

            public static string HasNewVersion
            {
                get { return Language.IsChinese ? "有新版本" : "HasNewVersion"; }
            }
        }

        public IArchitecture Architecture
        {
            get { return PackageKit.Interface; }
        }
    }
}