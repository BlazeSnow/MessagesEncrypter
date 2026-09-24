import { defineConfig } from 'vitepress'

//站点部署的根路径。
//线上地址为 https://messages.blazesnow.com/（根路径部署），base 保持 '/' 即可。
export const base = '/'

//各语言共享的站点配置
export const shared = defineConfig({
    //站点根路径
    base,
    //网页标题
    title: 'MessagesEncrypter',
    //网页地图
    sitemap: {
        hostname: 'https://messages.blazesnow.com'
    },
    //头文件配置
    head: [
        //网页logo
        ['link',
            { rel: 'icon', href: `${base}logo.ico` }
        ]
    ],
    //markdown配置
    markdown: {
        //显示行号
        lineNumbers: true,
    },
    //不参与站点构建的 markdown 文件（README 是仓库说明，不是站点页面）
    srcExclude: ['**/README.md'],
    //主题配置
    themeConfig: {
        //左上角logo
        logo: `${base}logo.ico`,
        //右边的小目录
        aside: true,
        //右边的小目录
        outline: [2, 4],
        //是否在markdown中的外部链接旁显示外部链接图标
        externalLinkIcon: false,
        //搜索内容显示本地化
        search: {
            //用自带的搜索功能
            provider: 'local',
            //其他选项
            options: {
                locales: {
                    root: {
                        translations: {
                            button: {
                                buttonText: '搜索文档',
                                buttonAriaLabel: '搜索文档',
                            },
                            modal: {
                                displayDetails: '显示文章的详细内容',
                                resetButtonTitle: '清除内容',
                                backButtonTitle: '返回',
                                noResultsText: '没有找到',
                                footer: {
                                    selectText: '选择',
                                    selectKeyAriaLabel: '选择',
                                    navigateText: '切换',
                                    navigateUpKeyAriaLabel: '向上',
                                    navigateDownKeyAriaLabel: '向下',
                                    closeText: '关闭',
                                    closeKeyAriaLabel: '关闭',
                                }
                            }
                        }
                    }
                }
            }
        },
        socialLinks: [
            { icon: 'github', link: 'https://github.com/BlazeSnow/MessagesEncrypter' },
        ],
    }
})
