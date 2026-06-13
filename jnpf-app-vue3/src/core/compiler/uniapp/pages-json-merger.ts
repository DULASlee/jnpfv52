/**
 * pages.json 智能合并器
 *
 * 编译器为每个实体生成 pages-{entity}.json 片段。
 * 合并器将所有片段合并为完整的 pages.json。
 *
 * @jnpf-generated v5.2.0 type=pages-merger platform=uniapp
 */

interface PageConfig {
  path: string;
  style?: Record<string, unknown>;
}

interface PagesFragment {
  pages?: PageConfig[];
  subPackages?: Array<{
    root: string;
    pages: PageConfig[];
  }>;
  _comment?: string;
}

/**
 * 合并多个 pages-{entity}.json 片段为完整 pages.json
 *
 * @param fragments — 每个实体的 JSON 字符串
 * @returns 完整 pages.json 字符串
 */
export function mergePagesJson(fragments: string[]): string {
  const allPages: PageConfig[] = [];
  const allSubPackages: PagesFragment["subPackages"] = [];

  for (const fragment of fragments) {
    try {
      const parsed: PagesFragment = JSON.parse(fragment);
      if (parsed.pages && Array.isArray(parsed.pages)) {
        allPages.push(...parsed.pages);
      }
      if (parsed.subPackages && Array.isArray(parsed.subPackages)) {
        allSubPackages.push(...parsed.subPackages);
      }
    } catch (e) {
      console.warn(
        "[pages-json-merger] 跳过解析失败的片段:",
        (e as Error).message,
      );
    }
  }

  const result: Record<string, unknown> = {
    pages: allPages,
    globalStyle: {
      navigationBarTitleText: "JNPF",
      navigationBarBackgroundColor: "#0083ff",
      navigationBarTextStyle: "white",
    },
  };

  if (allSubPackages.length > 0) {
    result.subPackages = allSubPackages;
  }

  return JSON.stringify(result, null, 2);
}
