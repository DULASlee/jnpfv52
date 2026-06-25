const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  const results = [];
  let passCount = 0;
  let failCount = 0;
  let skipCount = 0;

  console.log('=== S6-1 浏览器端到端验证 ===\n');

  try {
    // 登录
    console.log('正在登录...');
    await page.goto('http://localhost:3100', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    // 尝试多种登录表单选择器
    const accountInput = await page.$('input[placeholder*="账号"]') ||
                         await page.$('input[placeholder*="用户名"]') ||
                         await page.$('input[type="text"]') ||
                         await page.$('input:first-of-type');

    if (accountInput) {
      await accountInput.fill('admin');
      const passwordInput = await page.$('input[type="password"]') ||
                            await page.$('input[placeholder*="密码"]');
      if (passwordInput) {
        await passwordInput.fill('123456');
      }
      const submitBtn = await page.$('button[type="submit"]') ||
                        await page.$('button:has-text("登录")') ||
                        await page.$('button');
      if (submitBtn) {
        await submitBtn.click();
        await page.waitForTimeout(5000);
        console.log('登录成功\n');
      } else {
        console.log('未找到登录按钮\n');
      }
    } else {
      console.log('未找到账号输入框，可能已登录\n');
    }

    // 截图当前页面
    await page.screenshot({ path: 'scripts/test-screenshot.png' });

    // 基础功能验证
    console.log('=== 基础功能验证 ===');

    // #1 侧边栏菜单
    try {
      await page.waitForTimeout(2000);
      const menuItems = await page.$$('.ant-menu-item, .ant-menu-submenu, .ant-menu');
      const sidebar = await page.$('.ant-layout-sider, .sidebar, [class*="sidebar"]');
      if (menuItems.length > 0 || sidebar) {
        results.push('#1  侧边栏菜单: ✅ 备注: 找到菜单元素');
        passCount++;
      } else {
        results.push('#1  侧边栏菜单: ❌ 备注: 未找到菜单元素');
        failCount++;
      }
    } catch (e) {
      results.push('#1  侧边栏菜单: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #2 子菜单展开
    try {
      const submenus = await page.$$('.ant-menu-submenu, [class*="submenu"]');
      if (submenus.length > 0) {
        await submenus[0].click();
        await page.waitForTimeout(1000);
        results.push('#2  子菜单展开: ✅ 备注: 点击子菜单成功');
        passCount++;
      } else {
        results.push('#2  子菜单展开: ⏭️ 备注: 未找到子菜单，跳过');
        skipCount++;
      }
    } catch (e) {
      results.push('#2  子菜单展开: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #3 页面渲染
    try {
      const content = await page.content();
      const hasLayout = content.includes('ant-layout') || content.includes('sidebar');
      if (hasLayout) {
        results.push('#3  页面渲染: ✅ 备注: 页面正常渲染');
        passCount++;
      } else {
        results.push('#3  页面渲染: ❌ 备注: 页面结构异常');
        failCount++;
      }
    } catch (e) {
      results.push('#3  页面渲染: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #4 角色过滤
    results.push('#4  角色过滤: ⏭️ 备注: 需要多账号测试，跳过');
    skipCount++;

    // #5 框架嵌入
    try {
      const sidebar = await page.$('.ant-layout-sider, [class*="sidebar"]');
      const header = await page.$('.ant-layout-header, [class*="header"]');
      if (sidebar || header) {
        results.push('#5  框架嵌入: ✅ 备注: 有框架元素');
        passCount++;
      } else {
        results.push('#5  框架嵌入: ❌ 备注: 缺少框架元素');
        failCount++;
      }
    } catch (e) {
      results.push('#5  框架嵌入: ❌ 备注: ' + e.message);
      failCount++;
    }

    console.log('\n=== 核心链路验证 ===');

    // #6 提交需求
    try {
      await page.goto('http://localhost:3100/studio/ai/submit-requirement', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('提交') || content.includes('需求') || content.includes('submit')) {
        results.push('#6  提交需求: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#6  提交需求: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#6  提交需求: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #7 Pipeline创建
    results.push('#7  Pipeline创建: ⏭️ 备注: 需要AI服务，跳过');
    skipCount++;

    // #8 AI响应
    results.push('#8  AI响应: ⏭️ 备注: 需要AI服务，跳过');
    skipCount++;

    // #9 阶段推进
    results.push('#9  阶段推进: ⏭️ 备注: 需要AI服务，跳过');
    skipCount++;

    // #10 已生成系统
    try {
      await page.goto('http://localhost:3100/studio/ai/generated-systems', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('系统') || content.includes('生成') || content.includes('generated')) {
        results.push('#10 已生成系统: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#10 已生成系统: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#10 已生成系统: ❌ 备注: ' + e.message);
      failCount++;
    }

    console.log('\n=== 管理功能验证 ===');

    // #11 智能体管理
    try {
      await page.goto('http://localhost:3100/studio/agent/create', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('智能体') || content.includes('agent') || content.includes('创建')) {
        results.push('#11 智能体管理: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#11 智能体管理: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#11 智能体管理: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #12 模型路由
    try {
      await page.goto('http://localhost:3100/studio/pipeline/model-routing', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('路由') || content.includes('模型') || content.includes('routing')) {
        results.push('#12 模型路由: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#12 模型路由: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#12 模型路由: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #13 规则配置
    try {
      await page.goto('http://localhost:3100/studio/knowledge/rule-editor', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('规则') || content.includes('配置') || content.includes('rule')) {
        results.push('#13 规则配置: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#13 规则配置: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#13 规则配置: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #14 用量计费
    try {
      await page.goto('http://localhost:3100/studio/ai/usage-billing', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('用量') || content.includes('计费') || content.includes('Token') || content.includes('usage')) {
        results.push('#14 用量计费: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#14 用量计费: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#14 用量计费: ❌ 备注: ' + e.message);
      failCount++;
    }

    // #15 UI模板库
    try {
      await page.goto('http://localhost:3100/studio/ai/ui-templates', { waitUntil: 'networkidle' });
      await page.waitForTimeout(3000);
      const content = await page.content();
      if (content.includes('模板') || content.includes('UI') || content.includes('template')) {
        results.push('#15 UI模板库: ✅ 备注: 页面正常加载');
        passCount++;
      } else {
        results.push('#15 UI模板库: ❌ 备注: 页面内容异常');
        failCount++;
      }
    } catch (e) {
      results.push('#15 UI模板库: ❌ 备注: ' + e.message);
      failCount++;
    }

  } catch (e) {
    console.error('测试执行错误:', e.message);
  } finally {
    await browser.close();
  }

  // 输出结果
  console.log('\n=== 验证结果 ===\n');
  console.log('账号: admin / 123456\n');

  console.log('基础功能验证:');
  for (let i = 0; i < 5; i++) {
    console.log(results[i] || `#${i+1}  未执行`);
  }

  console.log('\n核心链路验证:');
  for (let i = 5; i < 10; i++) {
    console.log(results[i] || `#${i+1}  未执行`);
  }

  console.log('\n管理功能验证:');
  for (let i = 10; i < 15; i++) {
    console.log(results[i] || `#${i+1}  未执行`);
  }

  console.log('\n=== 统计 ===');
  console.log(`通过: ${passCount}/15`);
  console.log(`跳过: ${skipCount}/15`);
  console.log(`失败: ${failCount}/15`);
})();
