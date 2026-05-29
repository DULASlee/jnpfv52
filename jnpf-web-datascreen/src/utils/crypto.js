import CryptoJS from 'crypto-js'

export default class crypto {
  // 使用AesUtil.genAesKey()生成,需和后端配置保持一致
  static aesKey = "EY8WePvjM5GGwQzn";

  // 使用DesUtil.genDesKey()生成,需和后端配置保持一致
  static desKey = "jMVCBsFGDQr1USHo";

  /**
   * aes 加密方法
   * @param data
   * @returns {*}
   */
  static encrypt(data) {
    return this.encryptAES(data, this.aesKey);
  }

  /**
   * aes 解密方法
   * @param data
   * @returns {*}
   */
  static decrypt(data) {
    return this.decryptAES(data, this.aesKey);
  }

  /**
   * aes 加密方法
   */
  static encryptAES(str, cipherKey = '') {
    if (!cipherKey) cipherKey = this.aesKey
    const encryptedData = CryptoJS.AES.encrypt(str, CryptoJS.enc.Utf8.parse(cipherKey), {
      mode: CryptoJS.mode.ECB,
      padding: CryptoJS.pad.Pkcs7
    }).toString();
    const result = CryptoJS.enc.Hex.stringify(CryptoJS.enc.Base64.parse(encryptedData))
    return result
  }

  /**
   * aes 解密方法
   */
  static decryptAES(str, cipherKey = '') {
    if (!cipherKey) cipherKey = this.aesKey
    const hexStr = CryptoJS.enc.Base64.stringify(CryptoJS.enc.Hex.parse(str))
    const decryptedData = CryptoJS.AES.decrypt(hexStr, CryptoJS.enc.Utf8.parse(cipherKey), {
      mode: CryptoJS.mode.ECB,
      padding: CryptoJS.pad.Pkcs7
    }).toString(CryptoJS.enc.Utf8);
    return decryptedData
  }

  /**
   * des 加密方法，同java：DesUtil.encryptToBase64(text, desKey)
   */
  static encryptDES(data, key) {
    const keyHex = CryptoJS.enc.Utf8.parse(key);
    const encrypted = CryptoJS.DES.encrypt(data, keyHex, {
      mode: CryptoJS.mode.ECB,
      padding: CryptoJS.pad.Pkcs7
    });
    return encrypted.toString();
  }

  /**
   * des 解密方法，同java：DesUtil.decryptFormBase64(encryptBase64, desKey);
   */
  static decryptDES(data, key) {
    const keyHex = CryptoJS.enc.Utf8.parse(key);
    const decrypted = CryptoJS.DES.decrypt({
      ciphertext: CryptoJS.enc.Base64.parse(data)
    }, keyHex, {
      mode: CryptoJS.mode.ECB,
      padding: CryptoJS.pad.Pkcs7
    });
    return decrypted.toString(CryptoJS.enc.Utf8);
  }
}