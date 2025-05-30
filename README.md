# oimoBackup
- Windowsで作動するバックアップソフトです。

## かんたんな機能の紹介
- 指定したフォルダ以下を指定したファイル以下にコピーします。
- 同名のファイルがあり更新されていた場合は上書きします。
- ログに実行結果が表示されます。

## 注意点

- **バックアップは重要なデータ操作です。意図しないデータの上書きや消失を防ぐため、必ず事前に空のフォルダなどでテストを行い、動作を確認した上で、自己責任でご利用ください。**
- 初回バックアップ時や、非常に多くのファイル・大容量のファイルを扱う場合、処理に時間がかかることがあります。
- OSや他のプログラムが使用中のファイル、アクセス権限のないファイルなどは、正常にコピーできない場合があります。
- バックアップ元のフォルダ構成を変更した場合、バックアップ先のフォルダ構成は変更前の状態で、新しくフォルダが作成されます。ご注意ください。
- 大幅にフォルダ構成を変更した場合は、新しくバックアップ先を用意するのがオススメです。

## 使用技術
このアプリケーションは以下の技術を使用しています。

* C#
* WPF (.NET)
* Material Design In XAML Toolkit ([MIT License](https://github.com/MaterialDesignInXAML/MaterialDesignInXAMLToolkit/blob/master/LICENSE))


# 作者
oimo @亜寝帯

【C#】バックアップソフトを作った https://subsleepical.com/2025/04/20/%e3%80%90c%e3%80%91%e3%83%90%e3%83%83%e3%82%af%e3%82%a2%e3%83%83%e3%83%97%e3%82%bd%e3%83%95%e3%83%88%e3%82%92%e4%bd%9c%e3%81%a3%e3%81%9f/