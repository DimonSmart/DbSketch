create schema if not exists northwind;

create table northwind.customers (
    customer_id text primary key,
    company_name text not null,
    contact_name text null
);

comment on table northwind.customers is 'Companies and contacts that place orders.';
comment on column northwind.customers.customer_id is 'Stable customer code used on orders.';
comment on column northwind.customers.company_name is 'Legal or trading company name.';
comment on column northwind.customers.contact_name is 'Primary contact person for the customer.';

create table northwind.employees (
    employee_id integer primary key,
    last_name text not null,
    first_name text not null,
    reports_to integer null,
    constraint fk_employees_reports_to
        foreign key (reports_to)
        references northwind.employees(employee_id)
);

comment on table northwind.employees is 'Sales employees and their reporting hierarchy.';
comment on column northwind.employees.employee_id is 'Internal employee identifier.';
comment on column northwind.employees.last_name is 'Employee family name.';
comment on column northwind.employees.first_name is 'Employee given name.';
comment on column northwind.employees.reports_to is 'Manager employee id for hierarchy links.';

create table northwind.categories (
    category_id integer primary key,
    category_name text not null
);

comment on table northwind.categories is 'Product categories used for catalog grouping.';
comment on column northwind.categories.category_id is 'Category identifier referenced by products.';
comment on column northwind.categories.category_name is 'Display name for the category.';

create table northwind.products (
    product_id integer primary key,
    product_name text not null,
    category_id integer not null,
    unit_price numeric(10, 2) null,
    constraint fk_products_categories
        foreign key (category_id)
        references northwind.categories(category_id)
);

comment on table northwind.products is 'Sellable catalog items grouped by category.';
comment on column northwind.products.product_id is 'Product identifier used in order lines.';
comment on column northwind.products.product_name is 'Customer-facing product name.';
comment on column northwind.products.category_id is 'Category that classifies the product.';
comment on column northwind.products.unit_price is 'Current catalog price per unit.';

create table northwind.orders (
    order_id integer primary key,
    customer_id text not null,
    employee_id integer null,
    order_date timestamp null,
    constraint fk_orders_customers
        foreign key (customer_id)
        references northwind.customers(customer_id),
    constraint fk_orders_employees
        foreign key (employee_id)
        references northwind.employees(employee_id)
);

comment on table northwind.orders is 'Customer purchase orders handled by employees.';
comment on column northwind.orders.order_id is 'Order header identifier.';
comment on column northwind.orders.customer_id is 'Customer that placed the order.';
comment on column northwind.orders.employee_id is 'Employee responsible for the order.';
comment on column northwind.orders.order_date is 'Date and time when the order was created.';

create table northwind.order_details (
    order_id integer not null,
    product_id integer not null,
    unit_price numeric(10, 2) not null,
    quantity smallint not null,
    discount numeric(4, 2) not null default 0,
    constraint pk_order_details
        primary key (order_id, product_id),
    constraint fk_order_details_orders
        foreign key (order_id)
        references northwind.orders(order_id),
    constraint fk_order_details_products
        foreign key (product_id)
        references northwind.products(product_id)
);

comment on table northwind.order_details is 'Line items that connect orders to purchased products.';
comment on column northwind.order_details.order_id is 'Order that owns this line item.';
comment on column northwind.order_details.product_id is 'Product sold on this line item.';
comment on column northwind.order_details.unit_price is 'Unit price captured at order time.';
comment on column northwind.order_details.quantity is 'Number of product units ordered.';
comment on column northwind.order_details.discount is 'Discount fraction applied to the line.';
