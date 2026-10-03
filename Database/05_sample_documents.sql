-- ============================================
-- Sample Knowledge Base for Support Agent
-- ============================================

INSERT INTO documents (title, source_type, content) VALUES
('Return Policy', 'policy', 
'Our return policy allows customers to return any product within 30 days of purchase for a full refund. The product must be in its original packaging and unused condition. To initiate a return, customers should contact support@querynex.ai with their order number. Refunds are processed within 5-7 business days after we receive the returned item. Shipping costs for returns are covered by QueryNex for defective products, but customers are responsible for return shipping on non-defective items. Digital products and gift cards are non-refundable.'),

('Shipping Information', 'policy',
'We offer standard shipping (5-7 business days), express shipping (2-3 business days), and same-day delivery in select cities. Standard shipping is free on orders over ₹5000. Express shipping costs ₹299 and same-day delivery costs ₹499. We currently ship across India, USA, UK, and Australia. International orders may be subject to customs duties and import taxes, which are the customer responsibility. Tracking information is emailed within 24 hours of shipment.'),

('Warranty Policy', 'policy',
'All electronics come with a 1-year manufacturer warranty. Extended warranties of 2 or 3 years can be purchased at checkout. Warranty covers manufacturing defects but not physical damage, water damage, or unauthorized repairs. To claim warranty, contact support with your order number and photos of the issue. Replacement units are shipped within 3-5 business days after warranty approval.'),

('Payment Methods', 'faq',
'We accept all major credit cards (Visa, Mastercard, American Express), debit cards, UPI, net banking, PayPal, and cash on delivery (COD) for orders under ₹50,000. EMI options are available on orders over ₹10,000 with select banks. All payments are processed through secure PCI-DSS compliant gateways. We do not store credit card information on our servers.'),

('Account & Login Help', 'faq',
'To reset your password, click "Forgot Password" on the login page and enter your registered email. You will receive a password reset link within 5 minutes. If you don not receive it, check your spam folder. For account security, we recommend enabling two-factor authentication. If you suspect unauthorized access, contact support immediately to lock your account.'),

('Contacting Support', 'faq',
'Our support team is available 24/7 via live chat, email at support@querynex.ai, and phone at +91-9876543210. Average response time is under 2 hours for email and instant for live chat. For urgent issues like payment failures or missing shipments, please use the live chat option. Our support team is multilingual and can assist in English, Hindi, Tamil, and Telugu.'),

('Order Tracking', 'faq',
'Once your order ships, you will receive a tracking number via email and SMS. Track your order using the link in the email or by logging into your account and visiting the Orders section. Delivery updates are sent at every stage: picked up, in transit, out for delivery, and delivered. If tracking has not updated in 48 hours, contact support.'),

('Cancellation Policy', 'policy',
'Orders can be cancelled free of charge before they are shipped. Once shipped, orders cannot be cancelled but can be returned after delivery following our return policy. To cancel an order, go to Orders, select the order, and click Cancel. Refunds for cancelled orders are processed within 3 business days. Pre-orders can be cancelled anytime before the release date.');

-- Sample support tickets
INSERT INTO support_tickets (customer_id, subject, description, status, priority) VALUES
(1, 'Order not received', 'My order was supposed to arrive 3 days ago but I still have not received it.', 'open', 'high'),
(3, 'Wrong item delivered', 'I ordered a MacBook Air but received a Dell laptop instead.', 'in_progress', 'urgent'),
(7, 'Refund not processed', 'My return was approved 2 weeks ago but refund has not arrived.', 'open', 'high'),
(10, 'Payment failed', 'Tried to place an order 3 times but payment keeps failing.', 'resolved', 'normal');