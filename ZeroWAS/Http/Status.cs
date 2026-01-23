using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public enum Status
    {
        /// <summary>
        /// 继续
        /// </summary>
        Continue = 100,
        /// <summary>
        /// 切换协议
        /// </summary>
        Switching_Protocol = 101,
        /// <summary>
        /// 成功
        /// </summary>
        OK = 200,
        /// <summary>
        /// 已创建
        /// </summary>
        Created = 201,
        /// <summary>
        /// 已接受
        /// </summary>
        Accepted = 202,
        /// <summary>
        /// 未授权信息
        /// </summary>
        Non__Authoritative_Information = 203,
        /// <summary>
        /// 无内容
        /// </summary>
        No_Content = 204,
        /// <summary>
        /// 重置内容
        /// </summary>
        Reset_Content = 205,
        /// <summary>
        /// 部分内容
        /// </summary>
        Partial_Content = 206,
        /// <summary>
        /// 多种选择
        /// </summary>
        Multiple_Choice = 300,
        /// <summary>
        /// 永久移动
        /// </summary>
        Moved_Permanently = 301,
        /// <summary>
        /// 临时移动
        /// </summary>
        Found = 302,
        /// <summary>
        /// 查看其他位置
        /// </summary>
        See_Other = 303,
        /// <summary>
        /// 未修改
        /// </summary>
        Not_Modified = 304,
        /// <summary>
        /// 使用代理
        /// </summary>
        Use_Proxy = 305,
        /// <summary>
        /// 未使用
        /// </summary>
        unused = 306,
        /// <summary>
        /// 临时重定向(同302)
        /// </summary>
        Temporary_Redirect = 307,
        /// <summary>
        /// 永久重定向(同301)
        /// </summary>
        Permanent_Redirect = 308,
        /// <summary>
        /// 错误请求
        /// </summary>
        Bad_Request = 400,
        /// <summary>
        /// 未授权
        /// </summary>
        Unauthorized = 401,
        /// <summary>
        /// 需要付款
        /// </summary>
        Payment_Required = 402,
        /// <summary>
        /// 禁止访问
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// 未找到
        /// </summary>
        Not_Found = 404,
        /// <summary>
        /// 不允许使用该方法
        /// </summary>
        Method_Not_Allowed = 405,
        /// <summary>
        /// 无法接受
        /// </summary>
        Not_Acceptable = 406,
        /// <summary>
        /// 要求代理身份验证
        /// </summary>
        Proxy_Authentication_Required = 407,
        /// <summary>
        /// 请求超时
        /// </summary>
        Request_Timeout = 408,
        /// <summary>
        /// 冲突
        /// </summary>
        Conflict = 409,
        /// <summary>
        /// 已失效
        /// </summary>
        Gone = 410,
        /// <summary>
        /// 需要内容长度头
        /// </summary>
        Length_Required = 411,
        /// <summary>
        /// 预处理失败
        /// </summary>
        Precondition_Failed = 412,
        /// <summary>
        /// 请求实体过长
        /// </summary>
        Request_Entity_Too_Large = 413,
        /// <summary>
        /// 请求网址过长
        /// </summary>
        Request__URI_Too_Long = 414,
        /// <summary>
        /// 媒体类型不支持
        /// </summary>
        Unsupported_Media_Type = 415,
        /// <summary>
        /// 请求范围不合要求
        /// </summary>
        Requested_Range_Not_Satisfiable = 416,
        /// <summary>
        /// 预期结果失败
        /// </summary>
        Expectation_Failed = 417,
        /// <summary>
        /// 请求被发送到了一个无法为该 Host 提供服务的服务器（HTTP 2.0 版本）
        /// </summary>
        Misdirected_Request = 421,
        /// <summary>
        /// 内部服务器错误
        /// </summary>
        Internal_Server_Error = 500,
        /// <summary>
        /// 未实现
        /// </summary>
        Implemented = 501,
        /// <summary>
        /// 网关错误
        /// </summary>
        Bad_Gateway = 502,
        /// <summary>
        /// 服务不可用
        /// </summary>
        Service_Unavailable = 503,
        /// <summary>
        /// 网关超时
        /// </summary>
        Gateway_Timeout = 504,
        /// <summary>
        /// HTTP版本不受支持
        /// </summary>
        HTTP_Version_Not_Supported = 505
    }
}
