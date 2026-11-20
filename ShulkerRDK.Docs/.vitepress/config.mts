import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'
import { lvt } from './shiki/lvt'

// https://vitepress.dev/reference/site-config
// @ts-ignore - vitepress-plugin-mermaid type incompatibility
export default withMermaid(
  defineConfig({
    title: "ShulkerRDK Docs",
    lang: "zh-CN",
    cleanUrls: true,
    base: "/ShulkerRDK/",
    description: "面向 Minecraft 的模块化低代码 SDK 与资源装配工具",
    head: [
      ['link', { rel: 'icon', type: 'image/svg+xml', href: '/ShulkerRDK/images/srdk.svg' }]
    ],
    markdown: {
      shikiSetup(lang) {
        lang.loadLanguage(lvt)
      }
    },
    themeConfig: {
      // https://vitepress.dev/reference/default-theme-config
      nav: [
        { text: '主页', link: '/' },
        { text: '快速开始', link: '/quickGuides/' },
        { text: '手册', link: '/brochure/overview' }
      ],

      sidebar: [
        {
          text: '快速开始',
          link: '/quickGuides/',
          items: [
            { text: '资源包', link: '/quickGuides/resourcepack' },
            { text: '整合包', link: '/quickGuides/modpack' }
          ]
        },
        {
          text: '手册',
          link: '/brochure/',
          items: [
            { text: '总览', link: '/brochure/overview' },
            { text: '交互与架构', link: '/brochure/interaction' },
            { text: '项目配置与扩展', link: '/brochure/configuration' },
            {
              text: '核心扩展',
              items: [
                { text: '指令', link: '/brochure/core/commands' },
                { text: 'Levitate', link: '/brochure/core/levitate' },
                { text: '启动行动', link: '/brochure/core/startupActions' }
              ]
            },
            {
              text: '第一方扩展',
              items: [
                { text: 'ModrinthPSK', link: '/brochure/shulker.modrinth/' },
                { text: 'Prismarine', link: '/brochure/shulker.prismarine/' },
                { text: 'ResourceMagick', link: '/brochure/shulker.magick/' },
                { text: 'AsepriteExtractor', link: '/brochure/shulker.ase/' },
                { text: 'ShulkerFFmpeg', link: '/brochure/shulker.ffmpeg/' },
                { text: 'ShulkerRRT', link: '/brochure/shulker.rrt/' }
              ]
            }
          ]
        }
      ],

      search: {
        provider: 'local',
        options: {
          translations: {
            button: {
              buttonText: '搜索文档',
              buttonAriaLabel: '搜索文档'
            },
            modal: {
              noResultsText: '无法找到相关结果',
              resetButtonTitle: '清除查询条件',
              footer: {
                selectText: '选择',
                navigateText: '切换'
              }
            }
          }
        }
      },

      socialLinks: [
        { icon: 'github', link: 'https://github.com/LiPolymer/ShulkerRDK' },
        { icon: {svg: `<svg role="img" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><title>GitLab</title><path d="m23.6004 9.5927-.0337-.0862L20.3.9814a.851.851 0 0 0-.3362-.405.8748.8748 0 0 0-.9997.0539.8748.8748 0 0 0-.29.4399l-2.2055 6.748H7.5375l-2.2057-6.748a.8573.8573 0 0 0-.29-.4412.8748.8748 0 0 0-.9997-.0537.8585.8585 0 0 0-.3362.4049L.4332 9.5015l-.0325.0862a6.0657 6.0657 0 0 0 2.0119 7.0105l.0113.0087.03.0213 4.976 3.7264 2.462 1.8633 1.4995 1.1321a1.0085 1.0085 0 0 0 1.2197 0l1.4995-1.1321 2.4619-1.8633 5.006-3.7489.0125-.01a6.0682 6.0682 0 0 0 2.0094-7.003z"/></svg>`}, link: 'https://gitlab.com/LiPolymer/ShulkerRDK' }
      ],

      logo: '/images/srdk.svg',

      docFooter: {
        prev: '上一页',
        next: '下一页'
      },

      sidebarMenuLabel: '菜单',
      returnToTopLabel: '返回顶部',
      darkModeSwitchLabel: '外观',

      outline: {
        level: [2, 6],
        label: '页面导航'
      }
    }
  })
)
