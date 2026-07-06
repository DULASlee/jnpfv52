import type { Page, Locator } from '@playwright/test';

/**
 * 提交需求页 Page Object
 *
 * 选择器策略（2026-07 自愈改造）：
 *   1. data-testid（最稳定，UI 重构不影响）
 *   2. getByRole（语义化，适合按钮/标签页）
 *   3. getByText（文案驱动，适合明确文案的元素）
 *   4. CSS class 仅作为兜底（标记 `fallback:` 注释）
 */
export class SubmitRequirementPage {
  readonly textarea: Locator;
  readonly sendBtn: Locator;
  readonly chatStream: Locator;
  readonly stageSidebar: Locator;
  readonly observatoryPanel: Locator;
  readonly deliverablesTab: Locator;
  readonly deliverableLinks: Locator;
  readonly attachmentBar: Locator;
  readonly fileInput: Locator;
  readonly attachBtn: Locator;

  constructor(readonly page: Page) {
    // ① data-testid 优先
    this.textarea = page.getByTestId('submit-requirement-textarea');
    this.sendBtn = page.getByTestId('submit-requirement-send-btn');
    this.chatStream = page.getByTestId('chat-stream');
    this.stageSidebar = page.getByTestId('panel-left');
    this.observatoryPanel = page.getByTestId('panel-right');
    this.attachBtn = page.getByTestId('submit-requirement-attach-btn');

    // ② 语义化选择器
    this.deliverablesTab = page.getByRole('tab', { name: '产物' });

    // ③ 文案驱动的复合选择器
    this.deliverableLinks = page
      .locator('.ir-deliverables-tab .item-row')
      .filter({ has: page.getByRole('button', { name: '下载' }) });

    // ④ fallback: 无 data-testid 的第三方/深层组件
    this.attachmentBar = page.locator('.ir-deliverables-tab');
    this.fileInput = page.locator('input[type="file"]').first();
  }

  async sendRequirement(text: string) {
    await this.textarea.fill(text);
    await this.sendBtn.click();
  }

  async uploadAttachment(filePath: string) {
    // 先用 attach-btn 触发文件选择器，再 setInputFiles
    await this.attachBtn.click();
    await this.fileInput.setInputFiles(filePath);
  }

  async openDeliverablesTab() {
    await this.deliverablesTab.click();
  }

  // ── 语义断言方法 ──

  gatePassedMessage() {
    return this.page.getByText('需求材料评估通过');
  }

  gateFailedMessage() {
    return this.page.getByText('需求材料评估未通过');
  }

  thinkingWorkflowBlock() {
    return this.page.getByText('推理与工作流');
  }

  deliverableInPanel(name: string | RegExp) {
    return this.page.locator('.ir-deliverables-tab').getByText(name);
  }

  deliverableButton(name: string | RegExp) {
    return this.deliverableLinks.filter({ hasText: name }).getByRole('button', { name: '下载' });
  }
}
