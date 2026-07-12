import type { Page, Locator } from '@playwright/test';

/**
 * 提交需求页 Page Object — 业务验收以页面可见结果为准。
 * 观测台（panel-right）当前可关闭，不得作为硬前置。
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
  readonly attList: Locator;

  constructor(readonly page: Page) {
    this.textarea = page.getByTestId('submit-requirement-textarea');
    this.sendBtn = page.getByTestId('submit-requirement-send-btn');
    this.chatStream = page.getByTestId('chat-stream');
    this.stageSidebar = page.getByTestId('panel-left');
    this.observatoryPanel = page.getByTestId('panel-right');
    this.attachBtn = page.getByTestId('submit-requirement-attach-btn');
    this.attList = page.locator('.input-bar .att-list');

    this.deliverablesTab = page.getByRole('tab', { name: '产物' });
    this.deliverableLinks = page
      .locator('.ir-deliverables-tab .item-row')
      .filter({ has: page.getByRole('button', { name: '下载' }) });
    this.attachmentBar = page.locator('.ir-deliverables-tab');
    this.fileInput = page.locator('input[type="file"]').first();
  }

  async sendRequirement(text: string) {
    await this.textarea.fill(text);
    await this.sendBtn.click();
  }

  async uploadAttachment(filePath: string) {
    await this.fileInput.setInputFiles(filePath);
  }

  async openDeliverablesTab() {
    await this.deliverablesTab.click();
  }

  gatePassedMessage() {
    return this.page.getByText('需求材料评估通过');
  }

  gateFailedMessage() {
    return this.page.getByText('需求材料评估未通过');
  }

  thinkingWorkflowBlock() {
    return this.page.getByText('推理与工作流');
  }

  attachmentParsedInChat() {
    return this.chatStream.getByText('已解析');
  }

  pmTotAllBranchesFailed() {
    return this.chatStream.getByText('全部分支产出无效');
  }

  objectObjectGarbage() {
    return this.chatStream.getByText('[object Object]');
  }

  skeletonConfirmCard() {
    return this.page.getByText(/确认.*骨架|IR-0|业务事件/).first();
  }

  deliverableInPanel(name: string | RegExp) {
    return this.page.locator('.ir-deliverables-tab').getByText(name);
  }

  deliverableButton(name: string | RegExp) {
    return this.deliverableLinks.filter({ hasText: name }).getByRole('button', { name: '下载' });
  }

  async waitPipelineIdInUrl(timeoutMs = 120_000): Promise<number> {
    await this.page.waitForFunction(
      () => {
        const m = location.hash.match(/pipelineId=(\d+)/);
        return m ? Number(m[1]) > 0 : false;
      },
      { timeout: timeoutMs },
    );
    const hash = await this.page.evaluate(() => location.hash);
    const m = hash.match(/pipelineId=(\d+)/);
    return Number(m?.[1] || 0);
  }
}
