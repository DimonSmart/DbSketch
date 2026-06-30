create schema if not exists northwind;

create table northwind.customers (
    customer_id text primary key,
    company_name text not null,
    contact_name text null
);

create table northwind.employees (
    employee_id integer primary key,
    last_name text not null,
    first_name text not null,
    reports_to integer null,
    constraint fk_employees_reports_to
        foreign key (reports_to)
        references northwind.employees(employee_id)
);

create table northwind.categories (
    category_id integer primary key,
    category_name text not null
);

create table northwind.products (
    product_id integer primary key,
    product_name text not null,
    category_id integer not null,
    unit_price numeric(10, 2) null,
    constraint fk_products_categories
        foreign key (category_id)
        references northwind.categories(category_id)
);

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
