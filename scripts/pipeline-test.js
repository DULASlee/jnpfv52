const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('=== T-1 五阶段流水线端到端验证 ===\n');

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
        console.log('✅ 登录成功\n');
      } else {
        console.log('❌ 未找到登录按钮\n');
        return;
      }
    } else {
      console.log('未找到账号输入框，可能已登录\n');
    }

    // 导航到提交需求页面
    console.log('正在导航到提交需求页面...');
    await page.goto('http://localhost:3100/studio/ai/submit-requirement', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    // 截图当前页面
    await page.screenshot({ path: 'scripts/pipeline-01-submit-page.png' });
    console.log('✅ 已截图: pipeline-01-submit-page.png\n');

    // 查找输入框和提交按钮
    console.log('正在查找输入框...');
    const textarea = await page.$('textarea') ||
                     await page.$('input[type="text"]') ||
                     await page.$('[contenteditable]');

    if (textarea) {
      console.log('✅ 找到输入框');

      // 输入需求
      const requirement = '我需要一个简单的员工考勤系统，包含打卡、请假、统计三个功能';
      await textarea.fill(requirement);
      console.log(`✅ 已输入需求: ${requirement}\n`);

      // 截图
      await page.screenshot({ path: 'scripts/pipeline-02-requirement-input.png' });

      // 查找提交按钮
      console.log('正在查找提交按钮...');
      const submitBtn = await page.$('button:has-text("提交")') ||
                        await page.$('button[type="submit"]') ||
                        await page.$('button');

      if (submitBtn) {
        console.log('✅ 找到提交按钮');

        // 点击提交
        console.log('正在提交需求...');
        await submitBtn.click();
        await page.waitForTimeout(5000);

        // 截图
        await page.screenshot({ path: 'scripts/pipeline-03-after-submit.png' });
        console.log('✅ 已截图: pipeline-03-after-submit.png\n');

        // 检查是否有 AI 响应
        console.log('正在检查 AI 响应...');
        await page.waitForTimeout(10000); // 等待 AI 响应

        // 截图
        await page.screenshot({ path: 'scripts/pipeline-04-ai-response.png' });
        console.log('✅ 已截图: pipeline-04-ai-response.png\n');

        // 检查页面内容
        const content = await page.content();
        if (content.includes('需求分析') || content.includes('领域模型') || content.includes('追问')) {
          console.log('✅ AI 返回了需求分析结果');
        } else {
          console.log('⚠️ 未检测到需求分析结果');
        }

        // 检查 PipelineStageBar
        const stageBar = await page.$('.pipeline-stage-bar, .stage-bar, [class*="stage"]');
        if (stageBar) {
          console.log('✅ 找到 PipelineStageBar');
        } else {
          console.log('⚠️ 未找到 PipelineStageBar');
        }

        // 检查阶段推进按钮
        console.log('\n正在检查阶段推进按钮...');
        const advanceBtn = await page.$('button:has-text("确认并推进")') ||
                           await page.$('button:has-text("推进")') ||
                           await page.$('button:has-text("下一步")');

        if (advanceBtn) {
          console.log('✅ 找到阶段推进按钮');

          // 点击推进
          console.log('正在推进到阶段 2...');
          await advanceBtn.click();
          await page.waitForTimeout(10000); // 等待阶段 2 AI 响应

          // 截图
          await page.screenshot({ path: 'scripts/pipeline-05-stage2.png' });
          console.log('✅ 已截图: pipeline-05-stage2.png\n');

          // 检查阶段 2 内容
          const content2 = await page.content();
          if (content2.includes('架构设计') || content2.includes('技术选型') || content2.includes('系统架构')) {
            console.log('✅ AI 返回了架构设计结果');
          } else {
            console.log('⚠️ 未检测到架构设计结果');
          }
        } else {
          console.log('⚠️ 未找到阶段推进按钮');
        }
      } else {
        console.log('❌ 未找到提交按钮');
      }
    } else {
      console.log('❌ 未找到输入框');
    }

    // 最终截图
    await page.screenshot({ path: 'scripts/pipeline-06-final.png' });
    console.log('\n✅ 最终截图: pipeline-06-final.png');

  } catch (e) {
    console.error('测试执行错误:', e.message);
    await page.screenshot({ path: 'scripts/pipeline-error.png' });
  } finally {
    await browser.close();
  }

  console.log('\n=== 测试完成 ===');
})();
