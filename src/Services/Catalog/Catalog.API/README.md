## Containerized eShop - Catalog Service
## 商品目录子服务
The Catalog Microservice HTTP API. This is a Data-Driven/CRUD microservice sample

Catalog microservice（目录微服务）维护着所有产品信息，包括库存、价格。所以该微服务的核心业务为：  

    产品信息的维护  
    库存的更新  
    价格的维护  


知识列表：
- [x] 讲过；- [] 计划 ；~~- [ ]~~ 搁置


- [x] 单层架构`CURD` + 事件总线；

- [x] 没有授权认证，不需要；
- [x] 没有仓储，efcore直接用；

- [x] `Swagger`；
- [x] `Serilog`；
- [x] 选项配置；
- [x] `Autofac`；
- [x] 跨域；
- [x] 单元测试+功能测试；  
- [x] `Grpc`；

- [x] `EFCore` + `Sql Server`；
- [x] `Restful api` + 状态码 ；
- [x] 充血模型；

- [ ] `Polly`  Retry机制；
- [ ] 压缩/解压文件；
- [ ] 下载图片；
- [ ] `IOptionsSnapshot`热更新；

- [ ] 集成事件/事件日志`EventLog`；
- [ ] 事件服务`EventService`；


- [ ] 事件总线`Eventbus`；
- [ ] `RabbitMQ`；  

~~- [ ] 自定义健康检测；~~  
~~- [ ] Azure；~~  
~~- [ ] K8s；~~  
~~- [ ] App监控；~~    


