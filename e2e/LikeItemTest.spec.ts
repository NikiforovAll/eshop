import { test, expect } from '@playwright/test';

test('Like and unlike an item toggles the count', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();

  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();

  const likeButton = page.locator('button.like-button');
  await expect(likeButton).toBeVisible();

  // Like the item
  await expect(likeButton).toHaveAttribute('title', 'Like');
  const initialCount = Number((await likeButton.textContent())!.trim());

  await likeButton.click();
  await expect(likeButton).toHaveAttribute('title', 'Unlike');
  await expect(likeButton).toHaveClass(/liked/);
  await expect(likeButton).toContainText(String(initialCount + 1));

  // Unlike the item to leave clean state
  await likeButton.click();
  await expect(likeButton).toHaveAttribute('title', 'Like');
  await expect(likeButton).not.toHaveClass(/liked/);
  await expect(likeButton).toContainText(String(initialCount));
});
