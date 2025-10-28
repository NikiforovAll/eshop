# Code Generation Use Cases for eShop

**Context**: These prompts are designed for generating new features, endpoints, domain logic, and integration points in the eShop microservices architecture. They represent realistic e-commerce enhancements that would be found in actual product backlogs.

## Effectiveness Tips

**What makes these prompts effective:**

1. **Clear feature description** - One sentence explaining what to build and why
2. **Concrete requirements** - Specific properties, validation rules, edge cases with examples
3. **Starting points** - Reference existing files/patterns to follow for consistency
4. **Architecture awareness** - Mention patterns (DDD, CQRS, event-driven, gRPC) and integration points

**Keep prompts focused on:**
- What to build (feature goal)
- Key components (models, endpoints, handlers)
- Critical business rules (validation, constraints)
- Where to start (existing patterns to follow)

**Avoid:**
- Step-by-step instructions (trust Claude Code to figure out implementation details)
- Excessive file paths and line numbers (provide key starting points only)
- Overly detailed explanations (let the code speak for itself)

---
## Prompt 1: Implement Order Cancellation with Inventory Restoration

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

## Prompt 2: Add Shipping Address Validation to Order Creation

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

## Prompt 3: Add Product Wish List Feature to Basket Service

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