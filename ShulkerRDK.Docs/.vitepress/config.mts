import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

// https://vitepress.dev/reference/site-config
export default withMermaid(
  defineConfig({
    title: "ShulkerRDK Docs",
    base: "/ShulkerRDK/",
    description: "下一代MC低代码内容开发工具",
    themeConfig: {
      // https://vitepress.dev/reference/default-theme-config
      nav: [
        { text: 'Home', link: '/' },
        { text: 'GitLab', link: 'https://gitlab.com/LiPolymer/ShulkerRDK' }
      ],

      sidebar: [
        {
          text: '快速开始',
          link: '/quickGuides',
          items: [
            { text: '资源包', link: '/quickGuides/resourcepack' },
            { text: '整合包', link: '/quickGuides/modpack' }
          ]
        },
        {
          text: '手册',
          items: [
            { text: '总览', link: '/brochure/overview' },
            {text: '交互与架构', link: '/brochure/interaction'}
          ]
        }
      ],

      socialLinks: [
        { icon: 'github', link: 'https://github.com/LiPolymer/ShulkerRDK' }
      ],

      logo: '/images/srdk.svg'
    }
  })
)