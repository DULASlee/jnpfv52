// DFDValidator 测试 - 覆盖所有 6 个 ERROR code
import { DFDValidator } from '../src/validators/DFDValidator';
import { DFDBuilder } from './helpers/builders';

describe('DFDValidator', () => {
  // ========================================================
  // 1. 父子图平衡
  // ========================================================
  describe('父子图平衡', () => {
    it('失败 DFD_NOT_DECOMPOSED:Level 0 过程未在 Level 1 分解', () => {
      const dfd = new DFDBuilder()
        .addLevel0Process('P1', '过程1', ['in1'], ['out1'])
        // ❌ 没有 addLevel1Process,Level 0 没有子过程
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_NOT_DECOMPOSED' })
      );
    });

    it('失败 DFD_BALANCE_MISMATCH:父图 IO 在子图找不到', () => {
      const dfd = new DFDBuilder()
        .addLevel0Process('P1', '过程1', ['in1'], ['out1'])
        .addLevel1Process('P1.1', '子过程', 'P1', ['in1'], ['out2'])  // ❌ out1 丢失
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_BALANCE_MISMATCH' })
      );
    });

    it('通过:父图 IO 完整覆盖', () => {
      const dfd = new DFDBuilder()
        .addLevel0Process('P1', '过程1', ['in1', 'in2'], ['out1'])
        .addLevel1Process('P1.1', '子过程1', 'P1', ['in1'], ['mid1'])
        .addLevel1Process('P1.2', '子过程2', 'P1', ['in2', 'mid1'], ['out1'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors.filter(e => e.code.startsWith('DFD_BALANCE'))).toHaveLength(0);
    });
  });

  // ========================================================
  // 2. 数据守恒(黑洞 + 奇迹)
  // ========================================================
  describe('数据守恒', () => {
    it('失败 DFD_BLACK_HOLE:输入流找不到来源', () => {
      const dfd = new DFDBuilder()
        .addProcess('P1', '过程1', ['orphan_input'], ['out1'])  // ❌ orphan_input 无来源
        .addProcess('P2', '过程2', ['in2'], ['orphan_input'])  // P2 输出 orphan_input,不算
        .build();
      // 实际上 P2 输出 orphan_input 是合法的,但需要排除 P1 自己
      // 这里 P2 输出 orphan_input,所以 P1 的输入 orphan_input 有来源
      // 让我重新设计:让 P1 输入是真正没来源的
      const dfd2 = new DFDBuilder()
        .addProcess('P1', '过程1', ['truly_orphan'], ['out1'])  // ❌ 没人输出 truly_orphan
        .addProcess('P2', '过程2', ['in2'], ['other_out'])
        .build();
      const result = new DFDValidator(dfd2).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_BLACK_HOLE' })
      );
    });

    it('失败 DFD_MIRACLE:输出流没消费者', () => {
      const dfd = new DFDBuilder()
        .addProcess('P1', '过程1', ['in1'], ['orphan_output'])  // ❌ 没人消费
        .addProcess('P2', '过程2', ['other_in'], ['out2'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_MIRACLE' })
      );
    });

    it('通过:所有流都有源有终', () => {
      // 用闭环内部流：每个流都被某个进程产出，也被某个进程消费
      const dfd = new DFDBuilder()
        .addProcess('P1', '过程1', ['feedback'], ['step1_out'])
        .addProcess('P2', '过程2', ['step1_out'], ['step2_out'])
        .addProcess('P3', '过程3', ['step2_out'], ['feedback'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors.filter(e => e.code === 'DFD_BLACK_HOLE')).toHaveLength(0);
      expect(result.errors.filter(e => e.code === 'DFD_MIRACLE')).toHaveLength(0);
    });
  });

  // ========================================================
  // 3. 过程必须有 IO
  // ========================================================
  describe('过程必须有 IO', () => {
    it('失败 DFD_NO_INPUT:过程无输入', () => {
      const dfd = new DFDBuilder()
        .addProcess('P1', '过程1', [], ['out1'])  // ❌ 无输入
        .addProcess('P2', '过程2', ['out1'], ['out2'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_NO_INPUT' })
      );
    });

    it('失败 DFD_NO_OUTPUT:过程无输出', () => {
      const dfd = new DFDBuilder()
        .addProcess('P1', '过程1', ['in1'], [])  // ❌ 无输出
        .addProcess('P2', '过程2', ['other'], ['in1'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.errors).toContainEqual(
        expect.objectContaining({ code: 'DFD_NO_OUTPUT' })
      );
    });
  });

  // ========================================================
  // 4. 综合
  // ========================================================
  describe('综合场景', () => {
    it('完整合法 DFD 应通过', () => {
      // 闭环内部流：所有流都被进程产出和消费，无外部端点
      const dfd = new DFDBuilder()
        .addLevel0Process('P1', '录入', ['feedback'], ['initial_data'])
        .addLevel0Process('P2', '校验', ['initial_data'], ['validated_data'])
        .addLevel1Process('P1.1', '子录入', 'P1', ['feedback'], ['initial_data'])
        .addLevel1Process('P2.1', '子校验', 'P2', ['initial_data'], ['validated_data'])
        .addProcess('P3', '存储', ['validated_data'], ['feedback'])
        .build();
      const result = new DFDValidator(dfd).validate();
      expect(result.passed).toBe(true);
    });
  });
});
