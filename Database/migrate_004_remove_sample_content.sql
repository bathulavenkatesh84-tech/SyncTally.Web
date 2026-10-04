-- Removes the seeded SAMPLE testimonial so the site never shows a made-up quote.
-- The Home page hides the testimonial section while these settings are empty.
-- When you have a real customer quote (with their permission), insert it again:
--   INSERT INTO cmr_admin.site_setting (setting_key, setting_value) VALUES
--     ('testimonial_quote',  '<real quote>'),
--     ('testimonial_author', '<Name, business, city>');
DELETE FROM cmr_admin.site_setting WHERE setting_key IN ('testimonial_quote', 'testimonial_author');
