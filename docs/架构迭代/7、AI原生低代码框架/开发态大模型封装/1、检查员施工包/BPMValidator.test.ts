// BPMValidator 测试
import { BPMValidator } from '../src/BPMValidator';
import { BPMBuilder } from './helpers/builders';

describe('BPMValidator', () => {
  it('失败 BPM_NODE_NO_DFD:BPM 节点绑定了不存在的 DFD 过程', () => {
    const bpm = new BPMBuilder()
      .addNode('N1', '扫工序', 'GhostProcess')  // ❌ DFD 里没有 GhostProcess
      .build();
    const dfdProcesses = [{ id: 'P1', name: '录入' }];
    const result = new BPMValidator(bpm, dfdProcesses).validate();
    expect(result.errors).toContainEqual(
      expect.objectContaining({ code: 'BPM_NODE_NO_DFD' })
    );
  });

  it('警告 BPM_DFD_NO_NODE:DFD 过程在 BPM 中无对应节点', () => {
    const bpm = new BPMBuilder()
      .addNode('N1', '扫工序', 'P1')  // P2 没节点
      .build();
    const dfdProcesses = [
      { id: 'P1', name: '录入' },
      { id: 'P2', name: '校验' },  // ❌ BPM 没节点
    ];
    const result = new BPMValidator(bpm, dfdProcesses).validate();
    expect(result.errors).toContainEqual(
      expect.objectContaining({ code: 'BPM_DFD_NO_NODE', severity: 'WARNING' })
    );
  });

  it('通过:所有 BPM 节点都对应 DFD 过程,反之亦然', () => {
    const bpm = new BPMBuilder()
      .addNode('N1', '扫工序', 'P1')
      .addNode('N2', '提交', 'P2')
      .build();
    const dfdProcesses = [
      { id: 'P1', name: '录入' },
      { id: 'P2', name: '提交' },
    ];
    const result = new BPMValidator(bpm, dfdProcesses).validate();
    expect(result.passed).toBe(true);
  });
});
