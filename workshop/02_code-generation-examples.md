# Code Generation Use Cases for eShop

**Context**: These prompts are designed for generating new features, endpoints, domain logic, and integration points in the eShop microservices architecture. They represent realistic e-commerce enhancements that would be found in actual product backlogs.

**Category**: Code Generation
**Sub Category**: Feature Implementation / API Development / Domain Logic / Integration Events

---

## Prompt 1: Add Discount Coupon System to Basket Service

```
Implement a coupon code discount system in Basket.API. Users can apply coupon codes ("SAVE10" = 10% off, "SAVE20" = 20% off, "VIP30" = 30% off) to their basket via a gRPC ApplyCoupon method.

Key requirements:
- Add CouponCode and DiscountPercentage properties to CustomerBasket model
- Create ApplyCoupon gRPC method with request/response messages in basket.proto
- Validate: non-empty code, not already applied, exists in hardcoded list
- Handle edge cases: empty basket fails, invalid code returns error
- Start with: BasketService.cs for authentication patterns, RedisBasketRepository for storage
```

---

## Prompt 2: Add Product Review System to Catalog Service

```
Add product reviews and ratings (1-5 stars) to Catalog.API. Customers can submit reviews with optional text (max 1000 chars), view paginated reviews per product, and see aggregate rating statistics.

Key requirements:
- ProductReview entity: ProductId, UserId, Rating (1-5), ReviewText, ReviewDate, IsVerifiedPurchase
- Update CatalogItem with Reviews navigation property, AverageRating, ReviewCount
- Three endpoints: POST reviews, GET reviews (paginated), GET rating stats (avg + distribution)
- Validation: authenticated users only, one review per product, rating 1-5
- Start with: CatalogApi.cs for endpoint patterns, CatalogItemEntityTypeConfiguration for EF setup, create migration
```

---

## Prompt 3: Implement Order Cancellation with Inventory Restoration

```
Enable customers to cancel orders before shipping in Ordering.API, with automatic inventory restoration in Catalog.API via integration events.

Key requirements:
- PUT /api/orders/{orderId}/cancel endpoint with optional CancellationReason
- Only allow cancellation for Submitted/AwaitingValidation status (not Paid/Shipped)
- OrderCancelledIntegrationEvent: OrderId, OrderItems (ProductId + Quantity), CancellationReason
- Catalog handler restores inventory by calling catalogItem.AddStock(quantity)
- Error handling: 404 if not found, 400 if wrong status, 403 if not owner
- Start with: Order.SetCancelledStatus (already exists), OrderCancelledDomainEvent, event-driven patterns from ProductPriceChanged
```

---

## Prompt 4: Add Low Stock Alert Integration Event

```
Publish a ProductLowStockIntegrationEvent when catalog item stock falls below RestockThreshold after orders are placed.

Key requirements:
- ProductLowStockIntegrationEvent: ProductId, ProductName, AvailableStock, RestockThreshold, RequiredRestockQuantity
- Trigger in OrderStatusChangedToPaidIntegrationEventHandler after RemoveStock() calls
- Only publish once per restock cycle using OnReorder flag (prevent spam)
- WebhookClient handler logs alert with structured logging
- Use outbox pattern: SaveEventAndCatalogContextChangesAsync + PublishThroughEventBusAsync
- Start with: CatalogItem.RemoveStock, ProductPriceChangedIntegrationEvent pattern, add unit tests
```

---

## Prompt 5: Add Shipping Address Validation to Order Creation

```
Validate shipping addresses in CreateOrderCommandHandler before creating orders in Ordering.API. Prevent orders with invalid or incomplete addresses.

Key requirements:
- IAddressValidationService with ValidateAddressAsync(Address) method
- Validation rules: Street (5-200 chars), City (2-100 chars), ZipCode (12345 or 12345-6789), Country, State required
- AddressValidationResult value object: IsValid, ValidationErrors list
- Inject service in CreateOrderCommandHandler, validate before creating Order aggregate
- Throw OrderingDomainException on validation failure (returns 400 Bad Request)
- Start with: Address value object, existing service patterns, register in Extensions.cs, add unit tests
```

---

## Prompt 6: Add Product Wish List Feature to Basket Service

```
Add a wish list feature to Basket.API where authenticated users can save products for future purchase, separate from their shopping basket. Store in Redis with 180-day expiration.

Key requirements:
- WishList model: BuyerId, Items (WishListItem[]), CreatedDate, LastModifiedDate
- WishListItem: ProductId, ProductName, PictureUrl, Price (snapshot), Notes (max 200 chars), AddedDate
- gRPC methods: GetWishList, AddToWishList (fetch details from Catalog.API), RemoveFromWishList, MoveToBasket
- Redis repository pattern with key "wishlist:{buyerId}", 180-day expiration
- Validation: authenticated only, no duplicate ProductIds, positive ProductId
- Start with: RedisBasketRepository for storage pattern, BasketService for authentication, basket.proto for gRPC definitions
```

---

## Prompt 7: Implement Dynamic Pricing with Time-based Discounts

```
Add time-based promotional pricing to Catalog.API (e.g., "20% off between 6 PM-midnight"). Products calculate effective price based on current time, day of week, and promotion priority.

Key requirements:
- ProductPromotion entity: PromotionName, DiscountPercentage (0-100), StartTime, EndTime (TimeSpan), DaysOfWeek, IsActive, Priority
- CatalogItem.GetEffectivePrice(currentTime) method: applies highest priority active promotion or returns base price
- Update GetItemById/GetAllItems to include Promotions and set PromotionalPrice property
- CRUD endpoints: GET/POST/PUT/DELETE promotions
- Validation: discount 0-100, StartTime < EndTime, Priority >= 0, valid day names
- Start with: CatalogItemEntityTypeConfiguration for EF setup, CatalogApi.cs for endpoint patterns, create migration
```

---

## Prompt 8: Add Order Status Tracking Webhook Notifications

```
Send real-time order status updates to external systems via webhooks in Webhooks.API when order status changes (Submitted, Paid, Shipped, Cancelled).

Key requirements:
- Event handlers for OrderStatusChangedTo[Submitted|Paid|Shipped|Cancelled]IntegrationEvent
- Payload: eventType, orderId, newStatus, previousStatus, timestamp, orderTotal, buyerId (JSON)
- IWebhooksSender service: POST to subscription URLs with HMAC-SHA256 signature (X-Webhook-Signature header)
- Retry logic: 3 attempts with exponential backoff (1s, 2s, 4s) using Polly
- WebhookDelivery model: track delivery history with status, attempts, response codes
- Additional endpoints: GET delivery history, POST test webhook
- Start with: existing Webhooks.API subscription system, Polly patterns in ServiceDefaults, event bus registration
```

---

## Effectiveness Tips

**What makes these prompts effective:**

1. **Clear feature description** - One sentence explaining what to build and why
2. **Concrete requirements** - Specific properties, validation rules, edge cases with examples
3. **Starting points** - Reference existing files/patterns to follow for consistency
4. **Essential details only** - Data types, constraints, key validation rules without exhaustive steps
5. **Architecture awareness** - Mention patterns (DDD, CQRS, event-driven, gRPC) and integration points

**Keep prompts focused on:**
- What to build (feature goal)
- Key components (models, endpoints, handlers)
- Critical business rules (validation, constraints)
- Where to start (existing patterns to follow)

**Avoid:**
- Step-by-step instructions (trust Claude Code to figure out implementation details)
- Excessive file paths and line numbers (provide key starting points only)
- Overly detailed explanations (let the code speak for itself)
