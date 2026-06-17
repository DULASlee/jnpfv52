-- PostgreSQL DDL 脚本（由 SQL Server 脚本自动转换 v2）
-- 转换目标: PostgreSQL 15+

CREATE TABLE base_advanced_query_scheme(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_match_logic varchar(20) NULL,
	f_condition_json text NULL,
	f_module_id varchar(50) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_sort_code bigint NULL,
	f_tenant_id varchar(50) NULL,
	f_inte_assistant integer NULL,
	f_zx_system_id varchar(50) NULL,
	f_flow_task_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__base_adv__2911CBED97CE517E PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_api_log(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_user_name varchar(100) NULL,
	f_type integer NULL,
	f_level integer NULL,
	f_ip_address varchar(50) NULL,
	f_ip_address_name varchar(50) NULL,
	f_request_url varchar(500) NULL,
	f_request_method varchar(50) NULL,
	f_request_duration integer NULL,
	f_json text NULL,
	f_plat_form varchar(500) NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_module_id varchar(50) NULL,
	f_module_name varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_browser varchar(50) NULL,
	f_request_param text NULL,
	f_request_target text NULL,
	f_login_mark integer NULL,
	f_login_type integer NULL,
	f_zx_system_id varchar(50) NULL,
	F_REQUEST_Body_Type varchar(255) NULL,
	F_REQUEST_Body text NULL,
	F_REQUEST_Headers text NULL,
	F_REQUEST_Result text NULL,
	F_Msg text NULL,
	F_Status integer NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__base_sys__2911CBED3C589CD7_copy1 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_app_data(
	f_id varchar(50) NOT NULL,
	f_object_type varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_object_data text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_system_id varchar(50) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_sort_code bigint NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_flow_task_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__base_app__2911CBED196C2D15 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_authorize(
	f_id varchar(50) NOT NULL,
	f_item_type varchar(50) NULL,
	f_item_id varchar(50) NULL,
	f_object_type varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	F_ENABLED_MARK integer NULL,
 CONSTRAINT PK__base_aut__2911CBEDA321B0F2 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_bill_rule(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_prefix varchar(50) NULL,
	f_date_format varchar(50) NULL,
	f_digit integer NULL,
	f_start_number varchar(50) NULL,
	f_example varchar(100) NULL,
	f_this_number integer NULL,
	f_output_number varchar(100) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_category varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_type integer NULL,
	f_random_digit integer NULL,
	f_random_type integer NULL,
	f_suffix varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_bil__2911CBED0E01B8C9 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_columns_purview(
	f_id varchar(50) NOT NULL,
	f_field_list text NULL,
	f_module_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_col__2911CBED38097D3E PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_common_fields(
	f_id varchar(50) NOT NULL,
	f_field_name varchar(50) NULL,
	f_data_type varchar(50) NULL,
	f_data_length varchar(50) NULL,
	f_allow_null integer NULL,
	f_field varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_com__2911CBEDDED261FB PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_common_words(
	f_id varchar(50) NOT NULL,
	f_system_ids varchar(4000) NULL,
	f_common_words_text varchar(4000) NULL,
	f_common_words_type integer NULL,
	f_sort_code bigint NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_com__2911CBED556D3BB6 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_data_interface(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_category varchar(50) NULL,
	f_type integer NULL,
	f_action integer NULL,
	f_has_page integer NULL,
	f_is_postposition integer NULL,
	f_data_config_json text NOT NULL,
	f_data_count_json text NOT NULL,
	f_data_echo_json text NOT NULL,
	f_data_exception_json text NULL,
	f_data_js_json text NOT NULL,
	f_parameter_json text NULL,
	f_field_json text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_dat__2911CBED94FC4080 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_data_interface_log(
	f_id varchar(50) NOT NULL,
	f_invok_id varchar(50) NOT NULL,
	f_invok_time timestamp NULL,
	f_user_id varchar(50) NULL,
	f_invok_ip varchar(50) NULL,
	f_invok_device varchar(500) NULL,
	f_invok_type varchar(50) NULL,
	f_invok_waste_time integer NULL,
	f_oauth_app_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_dat__2911CBEDD9B8DA97 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_data_interface_oauth(
	f_id varchar(50) NOT NULL,
	f_app_id varchar(200) NOT NULL,
	f_app_name varchar(50) NOT NULL,
	f_app_secret varchar(200) NOT NULL,
	f_verify_signature integer NULL,
	f_useful_life timestamp NULL,
	f_white_list text NULL,
	f_black_list text NULL,
	f_data_interface_ids text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_description varchar(500) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_dat__2911CBED636625BD PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_data_interface_user(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_user_key varchar(50) NULL,
	f_oauth_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_dat__2911CBEDD1F963AE PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_data_interface_variate(
	f_id varchar(50) NOT NULL,
	f_interface_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_expression varchar(500) NULL,
	f_value text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_dat__2911CBED24DB4885 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_db_link(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_db_type varchar(50) NULL,
	f_host varchar(50) NULL,
	f_port integer NULL,
	f_user_name varchar(50) NULL,
	f_password varchar(50) NULL,
	f_service_name varchar(50) NULL,
	f_description varchar(500) NULL,
	f_db_schema varchar(50) NULL,
	f_table_space varchar(50) NULL,
	f_oracle_param varchar(500) NULL,
	f_oracle_extend integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_db___2911CBED62F182F7 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_dictionary_data(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_simple_spelling varchar(500) NULL,
	f_is_default integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_dictionary_type_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_zx_datatype integer NULL,
 CONSTRAINT PK__base_dic__2911CBEDC0E51BDB PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_dictionary_type(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_is_tree integer NULL,
	f_type integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_zx_datatype integer NULL,
 CONSTRAINT PK__base_dic__2911CBEDBD15EE4F PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_file(
	f_id varchar(50) NOT NULL,
	f_file_version varchar(500) NULL,
	f_file_name varchar(500) NULL,
	f_type integer NULL,
	f_url varchar(500) NULL,
	f_old_file_version_id varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_fil__2911CBEDFD278C03 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_group(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_category varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__base_gro__2911CBED27A91BF3 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_im_content(
	f_id varchar(50) NOT NULL,
	f_send_user_id varchar(50) NULL,
	f_send_time timestamp NULL,
	f_receive_user_id varchar(50) NULL,
	f_receive_time timestamp NULL,
	f_content text NULL,
	f_content_type varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_im___2911CBED9549E764 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_im_reply(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_receive_user_id varchar(50) NULL,
	f_receive_time timestamp NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_im___2911CBEDF243E69C PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_integrate(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_template_json text NULL,
	f_trigger_type integer NULL,
	f_resultType integer NULL,
	f_type integer NULL,
	f_form_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_int__2911CBEDD7AFE8BC PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_integrate_node(
	f_id varchar(50) NOT NULL,
	f_task_id varchar(50) NULL,
	f_form_Id varchar(50) NULL,
	f_node_type varchar(50) NULL,
	f_start_time timestamp NULL,
	f_end_time timestamp NULL,
	f_error_msg text NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_node_next varchar(2000) NULL,
	f_result_type integer NULL,
	f_node_property_json text NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_parent_id varchar(50) NULL,
	f_is_retry integer NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_int__2911CBEDCFAF7174 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_integrate_queue(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(50) NULL,
	f_integrate_id varchar(200) NULL,
	f_execution_time timestamp NULL,
	f_state integer NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_description varchar(4000) NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_int__2911CBED8C955E92 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_integrate_task(
	f_id varchar(50) NOT NULL,
	f_process_id varchar(50) NULL,
	f_parent_time timestamp NULL,
	f_parent_id varchar(50) NULL,
	f_execution_time timestamp NULL,
	f_template_json text NULL,
	f_data text NULL,
	f_data_id varchar(50) NULL,
	f_type integer NULL,
	f_integrate_id varchar(200) NULL,
	f_result_type integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_int__2911CBEDD478624B PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_message(
	f_id varchar(50) NOT NULL,
	f_type integer NULL,
	f_title varchar(200) NULL,
	f_flow_type integer NULL,
	f_user_id varchar(50) NULL,
	f_is_read integer NULL,
	f_read_time timestamp NULL,
	f_read_count integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_body_text text NULL,
	f_zx_system_id varchar(50) NULL,
	f_enabled_mark integer NULL,
 CONSTRAINT PK__base_mes__2911CBED96244A9D PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_type integer NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_category varchar(50) NULL,
	f_url_address varchar(500) NULL,
	f_icon varchar(500) NULL,
	f_link_target varchar(50) NULL,
	f_is_button_authorize integer NULL,
	f_is_column_authorize integer NULL,
	f_is_data_authorize integer NULL,
	f_is_form_authorize integer NULL,
	f_module_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
	f_property_json text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBED98F45AA7 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module_authorize(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_type varchar(50) NULL,
	f_condition_symbol varchar(500) NULL,
	f_condition_text varchar(500) NULL,
	f_property_json text NULL,
	f_module_id varchar(50) NULL,
	f_field_rule integer NULL,
	f_child_table_key varchar(50) NULL,
	f_bind_table varchar(50) NULL,
	f_format varchar(20) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBED0B246DBD PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module_button(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_icon varchar(500) NULL,
	f_url_address varchar(500) NULL,
	f_property_json text NULL,
	f_module_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBEDD43C3762 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module_column(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_bind_table varchar(50) NULL,
	f_bind_table_name varchar(50) NULL,
	f_property_json text NULL,
	f_module_id varchar(50) NULL,
	f_field_rule integer NULL,
	f_child_table_key varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBEDB104A3C5 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module_form(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_property_json text NULL,
	f_module_id varchar(50) NULL,
	f_field_rule integer NULL,
	f_child_table_key varchar(50) NULL,
	f_bind_table varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBED825258BB PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_module_link(
	f_id varchar(50) NOT NULL,
	f_link_id varchar(50) NULL,
	f_link_tables varchar(200) NULL,
	f_module_id varchar(50) NULL,
	f_type integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBED9B8C8A2A PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_module_scheme(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(100) NULL,
	f_condition_json text NULL,
	f_condition_text varchar(500) NULL,
	f_description varchar(500) NULL,
	f_module_id varchar(50) NULL,
	f_all_data integer NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_match_logic varchar(50) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_mod__2911CBEDB7FD72B2 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_msg_account(
	f_id varchar(50) NOT NULL,
	f_category varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_addressor_name varchar(50) NULL,
	f_smtp_server varchar(50) NULL,
	f_smtp_port integer NULL,
	f_ssl_link integer NULL,
	f_smtp_user varchar(50) NULL,
	f_smtp_password varchar(50) NULL,
	f_channel integer NULL,
	f_sms_signature varchar(50) NULL,
	f_app_id varchar(50) NULL,
	f_app_secret varchar(500) NULL,
	f_end_point varchar(50) NULL,
	f_sdk_app_id varchar(50) NULL,
	f_app_key varchar(50) NULL,
	f_zone_name varchar(50) NULL,
	f_zone_param varchar(50) NULL,
	f_enterprise_id varchar(50) NULL,
	f_agent_id varchar(50) NULL,
	f_webhook_type integer NULL,
	f_webhook_address varchar(500) NULL,
	f_approve_type integer NULL,
	f_bearer varchar(500) NULL,
	f_user_name varchar(50) NULL,
	f_password varchar(50) NULL,
	f_sort_code bigint NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED74B3FB88 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_msg_monitor(
	f_id varchar(50) NOT NULL,
	f_account_id varchar(50) NULL,
	f_account_name varchar(50) NULL,
	f_account_code varchar(50) NULL,
	f_message_type varchar(50) NULL,
	f_message_source varchar(50) NULL,
	f_send_time timestamp NULL,
	f_message_template_id varchar(50) NULL,
	f_title varchar(200) NULL,
	f_receive_user text NULL,
	f_content text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED1A9AF3F3 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_msg_send(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_template_type varchar(50) NULL,
	f_message_source varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED89D5D046 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_msg_send_template(
	f_id varchar(50) NOT NULL,
	f_send_config_id varchar(50) NULL,
	f_message_type varchar(50) NULL,
	f_template_id varchar(50) NULL,
	f_account_config_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED31701201 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_msg_short_link(
	f_id varchar(50) NOT NULL,
	f_short_link varchar(200) NULL,
	f_real_pc_link varchar(500) NULL,
	f_real_app_link varchar(500) NULL,
	f_body_text text NULL,
	f_is_used integer NULL,
	f_click_num integer NULL,
	f_unable_num integer NULL,
	f_unable_time timestamp NULL,
	f_user_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBEDCCDFE2DE PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_msg_sms_field(
	f_id varchar(50) NOT NULL,
	f_template_id varchar(50) NULL,
	f_field_id varchar(50) NULL,
	f_sms_field varchar(50) NULL,
	f_field varchar(50) NULL,
	f_is_title integer NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_sort_code bigint NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED9B16E2EE PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_msg_template(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_template_type varchar(50) NULL,
	f_message_source varchar(50) NULL,
	f_message_type varchar(50) NULL,
	f_wx_skip varchar(50) NULL,
	f_xcx_app_id varchar(50) NULL,
	f_title varchar(50) NULL,
	f_content text NULL,
	f_template_code varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED24407668 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_msg_template_param(
	f_id varchar(50) NOT NULL,
	f_template_id varchar(50) NULL,
	f_field varchar(50) NULL,
	f_field_name varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_sort_code bigint NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBEDC37BB936 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_msg_wechat_user(
	f_id varchar(50) NOT NULL,
	f_gzh_id varchar(50) NULL,
	f_user_id varchar(50) NULL,
	f_open_id varchar(50) NULL,
	f_close_mark integer NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_msg__2911CBED553949E2 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_notice(
	f_id varchar(50) NOT NULL,
	f_title varchar(200) NULL,
	f_body_text text NULL,
	f_to_user_ids text NULL,
	f_cover_image text NULL,
	f_files text NULL,
	f_expiration_time timestamp NULL,
	f_category varchar(50) NULL,
	f_type integer NULL,
	f_send_config_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_not__2911CBEDA8DC3ADF PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_organize(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_category varchar(50) NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_manager_id varchar(50) NULL,
	f_property_json text NULL,
	f_organize_id_tree text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_org__2911CBEDECD9FDAB PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_organize_administrator(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_organize_id varchar(50) NULL,
	f_organize_type varchar(50) NULL,
	f_this_layer_add integer NULL,
	f_this_layer_edit integer NULL,
	f_this_layer_delete integer NULL,
	f_sub_layer_add integer NULL,
	f_sub_layer_edit integer NULL,
	f_sub_layer_delete integer NULL,
	f_this_layer_select integer NULL,
	f_sub_layer_select integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_manager_group varchar(500) NULL,
	F_ZX_SYSTEM_ID varchar(50) NULL,
 CONSTRAINT PK__base_org__2911CBEDB73A68EC PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_organize_relation(
	f_id varchar(50) NOT NULL,
	f_organize_id varchar(50) NULL,
	f_object_type varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_enabled_mark integer NULL,
 CONSTRAINT PK__base_org__2911CBEDAA4DF795 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_permission_group(
	F_Id varchar(50) NOT NULL,
	F_Full_Name varchar(200) NULL,
	F_En_Code varchar(200) NULL,
	F_Permission_Member varchar(4000) NULL,
	F_Sort_Code bigint NULL,
	F_Description varchar(500) NULL,
	F_Enabled_Mark integer NULL,
	F_Creator_Time timestamp NULL,
	F_Creator_User_Id varchar(50) NULL,
	F_Last_Modify_Time timestamp NULL,
	F_Last_Modify_User_Id varchar(50) NULL,
	F_Delete_Mark integer NULL,
	F_Delete_Time timestamp NULL,
	F_Delete_User_Id varchar(50) NULL,
	F_Tenant_Id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__base_per__2C6EC723680E04E4 PRIMARY KEY 
(
	F_Id 
) 
) 
;
CREATE TABLE base_portal(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_category varchar(50) NULL,
	f_type integer NULL,
	f_state integer NULL,
	f_custom_url varchar(500) NULL,
	f_link_type integer NULL,
	f_enabled_lock integer NULL,
	f_platform varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_platform_release varchar(100) NULL,
	f_system_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_por__2911CBEDD966285C PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_portal_data(
	f_id varchar(50) NOT NULL,
	f_portal_id varchar(50) NULL,
	f_platform varchar(50) NULL,
	f_form_data text NULL,
	f_system_id varchar(50) NULL,
	f_type varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_por__2911CBEDB4C6D593 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_portal_manage(
	f_id varchar(50) NOT NULL,
	f_portal_id varchar(50) NOT NULL,
	f_system_id varchar(50) NOT NULL,
	f_platform varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_por__2911CBEDEC1239F8 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_position(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_type varchar(50) NULL,
	f_property_json text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_organize_id varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_pos__2911CBEDF11A9A4A PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_print_log(
	f_id varchar(50) NOT NULL,
	f_print_num integer NULL,
	f_print_title varchar(255) NULL,
	f_print_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_pri__2911CBED0B82DC7D PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_print_template(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NOT NULL,
	f_en_code varchar(50) NOT NULL,
	f_category varchar(50) NOT NULL,
	f_type integer NOT NULL,
	f_db_link_id varchar(50) NOT NULL,
	f_sql_template text NULL,
	f_left_fields text NULL,
	f_print_template text NOT NULL,
	f_page_param text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_source_type integer NULL,
	f_interface_id varchar(50) NULL,
	f_parameter_json text NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_pri__2911CBEDDF445C96 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_province(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_quick_query varchar(100) NULL,
	f_type varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_pro__2911CBEDB90A9FA6 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_province_atlas(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_quick_query varchar(50) NULL,
	f_type varchar(50) NULL,
	f_division_code varchar(50) NULL,
	f_atlas_center varchar(128) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_pro__2911CBED843D6D35 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_role(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_type varchar(50) NULL,
	f_property_json text NULL,
	f_global_mark integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_rol__2911CBED9C531A04 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_schedule(
	f_id varchar(50) NOT NULL,
	f_category varchar(50) NULL,
	f_urgent integer NULL,
	f_title varchar(500) NULL,
	f_content text NULL,
	f_all_day integer NULL,
	f_start_day timestamp NULL,
	f_start_time varchar(50) NULL,
	f_end_day timestamp NULL,
	f_end_time varchar(50) NULL,
	f_duration integer NULL,
	f_color varchar(50) NULL,
	f_reminder_time integer NULL,
	f_reminder_type integer NULL,
	f_send_config_id varchar(50) NULL,
	f_send_config_name varchar(200) NULL,
	f_repetition integer NULL,
	f_repeat_time timestamp NULL,
	f_push_time timestamp NULL,
	f_group_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_files text NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_sch__2911CBED8940EBA6 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_schedule_log(
	f_id varchar(50) NOT NULL,
	f_category varchar(50) NULL,
	f_urgent integer NULL,
	f_title varchar(500) NULL,
	f_content text NULL,
	f_all_day integer NULL,
	f_start_day timestamp NULL,
	f_start_time varchar(50) NULL,
	f_end_day timestamp NULL,
	f_end_time varchar(50) NULL,
	f_duration integer NULL,
	f_color varchar(50) NULL,
	f_reminder_time integer NULL,
	f_reminder_type integer NULL,
	f_send_config_id varchar(50) NULL,
	f_send_config_name varchar(200) NULL,
	f_repetition integer NULL,
	f_repeat_time timestamp NULL,
	f_push_time timestamp NULL,
	f_group_id varchar(50) NULL,
	f_user_id text NULL,
	f_schedule_id varchar(50) NULL,
	f_operation_type varchar(1) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_sch__2911CBED67069D3C PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_schedule_user(
	f_id varchar(50) NOT NULL,
	f_schedule_id varchar(50) NULL,
	f_to_user_id varchar(50) NULL,
	f_type integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_sch__2911CBEDAAC03000 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_sign_img(
	f_id varchar(50) NOT NULL,
	f_sign_img text NULL,
	f_is_default integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_sig__2911CBED66E6B836 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_signature(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_icon text NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_description varchar(500) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL
)  
;
CREATE TABLE base_signature_user(
	f_id varchar(50) NOT NULL,
	f_signature_id varchar(50) NULL,
	f_user_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_description varchar(500) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL
) 
;
CREATE TABLE base_socials_users(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_social_type varchar(50) NULL,
	f_social_id varchar(100) NULL,
	f_social_name varchar(100) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_soc__2911CBEDEA0F42B3 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_syn_third_info(
	f_id varchar(50) NOT NULL,
	f_third_type integer NULL,
	f_data_type integer NULL,
	f_sys_obj_id varchar(50) NULL,
	f_third_obj_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_syn__2911CBED953E54C1 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_sys_config(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(50) NULL,
	f_key varchar(50) NULL,
	f_value text NULL,
	f_category varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	F_ENABLED_MARK integer NULL,
	f_zx_datatype integer NULL,
 CONSTRAINT PK__base_sys__2911CBED3F49ECA9 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_sys_log(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_user_name varchar(100) NULL,
	f_type integer NULL,
	f_level integer NULL,
	f_ip_address varchar(50) NULL,
	f_ip_address_name varchar(50) NULL,
	f_request_url varchar(500) NULL,
	f_request_method varchar(50) NULL,
	f_request_duration integer NULL,
	f_json text NULL,
	f_plat_form varchar(500) NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_module_id varchar(50) NULL,
	f_module_name varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_browser varchar(50) NULL,
	f_request_param text NULL,
	f_request_target text NULL,
	f_login_mark integer NULL,
	f_login_type integer NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_sys__2911CBED3C589CD7 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_system(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_icon varchar(200) NULL,
	f_is_main integer NULL,
	f_property_json text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_navigation_icon varchar(500) NULL,
	f_work_logo_icon varchar(500) NULL,
	f_workflow_enabled integer NULL,
	f_zx_system_id varchar(50) NULL,
	f_inte_assistant integer NULL,
	f_system_api varchar(255) NULL,
 CONSTRAINT PK__base_sys__2911CBED22F7044B PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_time_task(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_execute_type integer NULL,
	f_execute_content text NULL,
	f_execute_cycle_json text NULL,
	f_last_run_time timestamp NULL,
	f_next_run_time timestamp NULL,
	f_run_count integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_tim__2911CBED2EF9E67A PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_time_task_log(
	f_id varchar(50) NOT NULL,
	f_task_id varchar(50) NULL,
	f_run_time timestamp NULL,
	f_run_result integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_tim__2911CBED9958089B PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_user(
	f_id varchar(50) NOT NULL,
	f_account varchar(50) NULL,
	f_real_name varchar(50) NULL,
	f_quick_query varchar(100) NULL,
	f_nick_name varchar(50) NULL,
	f_head_icon text NULL,
	f_gender integer NULL,
	f_birthday timestamp NULL,
	f_mobile_phone varchar(20) NULL,
	f_tele_phone varchar(20) NULL,
	f_landline varchar(50) NULL,
	f_email varchar(50) NULL,
	f_nation varchar(50) NULL,
	f_native_place varchar(50) NULL,
	f_entry_date timestamp NULL,
	f_certificates_type varchar(50) NULL,
	f_certificates_number varchar(50) NULL,
	f_education varchar(50) NULL,
	f_urgent_contacts varchar(50) NULL,
	f_urgent_tele_phone varchar(50) NULL,
	f_postal_address varchar(500) NULL,
	f_signature varchar(500) NULL,
	f_password varchar(50) NULL,
	f_secretkey varchar(50) NULL,
	f_first_log_time timestamp NULL,
	f_first_log_ip varchar(50) NULL,
	f_prev_log_time timestamp NULL,
	f_prev_log_ip varchar(50) NULL,
	f_last_log_time timestamp NULL,
	f_last_log_ip varchar(50) NULL,
	f_log_success_count integer NULL,
	f_log_error_count integer NULL,
	f_change_password_date timestamp NULL,
	f_language varchar(50) NULL,
	f_theme varchar(50) NULL,
	f_common_menu text NULL,
	f_is_administrator integer NULL,
	f_property_json text NULL,
	f_manager_id varchar(50) NULL,
	f_organize_id varchar(50) NULL,
	f_position_id varchar(50) NULL,
	f_role_id text NULL,
	f_portal_id text NULL,
	f_lock_mark integer NULL,
	f_unlock_time timestamp NULL,
	f_group_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
	f_handover_mark integer NULL,
	f_app_system_id varchar(50) NULL,
	f_ding_job_number varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_handover_userid varchar(100) NULL,
	f_rank varchar(50) NULL,
	f_openId varchar(50) NULL,
	f_is_dev integer NULL,
	f_biz_system_Id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__base_use__2911CBED65098DC1 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_user_device(
	f_id varchar(50) NOT NULL,
	f_client_id varchar(50) NULL,
	f_user_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_use__2911CBEDAD4171B6 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_user_old_password(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_account varchar(50) NULL,
	f_old_password varchar(50) NULL,
	f_secretkey varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_use__2911CBEDDD6A83EA PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_user_relation(
	f_id varchar(50) NOT NULL,
	f_user_id varchar(50) NULL,
	f_object_type varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
	f_enabled_mark integer NULL,
 CONSTRAINT PK__base_use__2911CBED183DBB0A PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE base_visual_dev(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_state integer NULL,
	f_type integer NULL,
	f_tables_data text NULL,
	f_category varchar(50) NULL,
	f_form_data text NULL,
	f_column_data text NULL,
	f_db_link_id varchar(50) NULL,
	f_web_type integer NULL,
	f_flow_id varchar(50) NULL,
	f_app_column_data text NULL,
	f_enable_flow integer NULL,
	f_interface_id varchar(50) NULL,
	f_interface_param text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_parent_id varchar(50) NULL,
	f_platform_release varchar(100) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_vis__2911CBED76B98712 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_visual_filter(
	f_id varchar(50) NOT NULL,
	f_module_id varchar(50) NULL,
	f_config text NULL,
	f_config_app text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_vis__2911CBED9CFC0F27 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_visual_link(
	f_id varchar(50) NOT NULL,
	f_short_link varchar(500) NULL,
	f_form_use integer NULL,
	f_form_link varchar(500) NULL,
	f_form_pass_use integer NULL,
	f_form_password varchar(500) NULL,
	f_column_use integer NULL,
	f_column_link varchar(500) NULL,
	f_column_pass_use integer NULL,
	f_column_password varchar(500) NULL,
	f_column_condition text NULL,
	f_column_text text NULL,
	f_real_pc_link varchar(500) NULL,
	f_real_app_link varchar(500) NULL,
	f_user_id varchar(50) NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_vis__2911CBEDE62DF985 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE base_visual_release(
	f_id varchar(50) NOT NULL,
	f_full_name varchar(200) NULL,
	f_en_code varchar(200) NULL,
	f_state integer NULL,
	f_type integer NOT NULL,
	f_tables_data text NULL,
	f_category varchar(50) NULL,
	f_form_data text NULL,
	f_column_data text NULL,
	f_db_link_id varchar(50) NULL,
	f_web_type integer NULL,
	f_flow_id varchar(50) NULL,
	f_app_column_data text NULL,
	f_enable_flow integer NULL,
	f_interface_id varchar(50) NULL,
	f_interface_param text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_inte_assistant integer NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__base_vis__2911CBEDD94455AE PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE blade_visual(
	id varchar(64) NOT NULL,
	title varchar(255) NULL,
	background_url text NULL,
	category integer NULL,
	password varchar(255) NULL,
	create_user varchar(64) NULL,
	create_dept varchar(64) NULL,
	create_time timestamp NULL,
	update_user varchar(64) NULL,
	update_time timestamp NULL,
	status integer NOT NULL,
	is_deleted integer NOT NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83FD9A4EDB0 PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE blade_visual_category(
	id varchar(64) NOT NULL,
	category_key varchar(12) NULL,
	category_value varchar(64) NULL,
	is_deleted integer NOT NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83F7A6B36F1 PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE blade_visual_component(
	id bigint NOT NULL,
	name varchar(50) NULL,
	content text NULL,
	"type" integer NULL,
	img varchar(255) NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83FDE464D94 PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE blade_visual_config(
	id varchar(64) NOT NULL,
	visual_id varchar(64) NULL,
	detail text NULL,
	component text NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83F2ED1EAD0 PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE blade_visual_db(
	id varchar(64) NOT NULL,
	name varchar(100) NULL,
	driver_class varchar(100) NULL,
	url varchar(500) NULL,
	username varchar(50) NULL,
	password varchar(50) NULL,
	remark varchar(255) NULL,
	create_user varchar(64) NULL,
	create_dept varchar(64) NULL,
	create_time timestamp NULL,
	update_user varchar(64) NULL,
	update_time timestamp NULL,
	status integer NULL,
	is_deleted integer NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83FBF8B1D88 PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE blade_visual_glob(
	id varchar(64) NOT NULL,
	globalName varchar(255) NULL,
	globalKey varchar(255) NULL,
	globalValue varchar(4000) NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL
) 
;
CREATE TABLE blade_visual_map(
	id varchar(64) NOT NULL,
	name varchar(255) NULL,
	data text NULL,
	f_tenant_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83F3FBAAB87 PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE blade_visual_record(
	id varchar(64) NOT NULL,
	name varchar(255) NULL,
	url varchar(255) NULL,
	dataType integer NULL,
	dataMethod varchar(255) NULL,
	dataHeader varchar(4000) NULL,
	data varchar(4000) NULL,
	dataQuery varchar(4000) NULL,
	dataQueryType varchar(255) NULL,
	dataFormatter text NULL,
	proxy integer NULL,
	wsUrl varchar(255) NULL,
	dbsql varchar(255) NULL,
	fsql text NULL,
	result varchar(255) NULL,
	f_tenant_id varchar(50) NULL,
	mqttUrl varchar(255) NULL,
	mqttConfig text NULL,
	f_system_id varchar(50) NULL,
 CONSTRAINT PK__blade_vi__3213E83FD4265EBF PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE data_report(
	F_Id varchar(50) NOT NULL,
	F_CategoryId varchar(50) NULL,
	F_FullName varchar(50) NULL,
	F_Content text NULL,
	F_EnCode varchar(50) NULL,
	F_SortCode varchar(50) NULL,
	F_EnabledMark integer NULL,
	F_Description varchar(500) NULL,
	F_CreatorTime timestamp NULL,
	F_CreatorUserId varchar(50) NULL,
	F_LastModifyTime timestamp NULL,
	F_LastModifyUserId varchar(50) NULL,
	F_DeleteMark integer NULL,
	F_DeleteTime timestamp NULL,
	F_DeleteUserId varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__data_rep__2C6EC7230933367B PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE Demo_ExcelTest(
	Id varchar(50) NOT NULL,
	OrderId varchar(50) NULL,
	Name varchar(50) NULL,
	Price decimal(18, 2) NULL,
	Count integer NULL,
	f_inte_assistant integer NULL,
	createDate timestamp NULL,
	remark varchar(50) NULL,
	ProductClass1 varchar(50) NULL,
	ProductClass2 varchar(50) NULL,
	ProductClass3 varchar(50) NULL,
	ProductClass4 varchar(50) NULL,
	ProductClass5 varchar(50) NULL,
	ProductClass6 varchar(50) NULL,
 CONSTRAINT PK_Demo_ExcelTest PRIMARY KEY 
(
	Id 
) 
) 
;
CREATE TABLE Demo_Order(
	Id varchar(50) NOT NULL,
	OrderNum varchar(50) NULL,
	Amount decimal(18, 2) NULL,
	CreateBy varchar(50) NULL,
	CreateTime timestamp NULL,
	Remarks text NULL,
	f_inte_assistant integer NULL,
	OrderType varchar(50) NULL,
	VirtualField1 varchar(50) NULL,
	VirtualField2 varchar(50) NULL,
	VirtualField3 varchar(50) NULL,
	test001 varchar(50) NULL,
	ddd varchar(50) NULL,
	f_flow_task_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK_Demo_Order PRIMARY KEY 
(
	Id 
) 
)  
;
CREATE TABLE Demo_OrderDetail(
	Id varchar(50) NOT NULL,
	OrderId varchar(50) NULL,
	Name varchar(50) NULL,
	Price decimal(18, 2) NULL,
	Count integer NULL,
	f_inte_assistant integer NULL,
	createDate timestamp NULL,
	remark varchar(50) NULL,
	ProductClass varchar(50) NULL,
 CONSTRAINT PK_Demo_OrderDetail PRIMARY KEY 
(
	Id 
) 
) 
;
CREATE TABLE ext_big_data(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_big___2911CBED0BB82F2F PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_customer(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(50) NULL,
	f_customer_name varchar(50) NULL,
	f_address varchar(255) NULL,
	f_full_name varchar(50) NULL,
	f_contact_tel varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__ext_cust__2911CBED01F0D899 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_document(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_type integer NULL,
	f_full_name varchar(200) NULL,
	f_file_path varchar(2000) NULL,
	f_file_size varchar(50) NULL,
	f_file_extension varchar(50) NULL,
	f_read_count integer NULL,
	f_is_share integer NULL,
	f_share_time timestamp NULL,
	f_upload_url varchar(255) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_docu__2911CBEDD06233E9 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_document_share(
	f_id varchar(50) NOT NULL,
	f_document_id varchar(50) NULL,
	f_share_user_id varchar(50) NULL,
	f_share_time timestamp NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_docu__2911CBED2252295C PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_email_config(
	f_id varchar(50) NOT NULL,
	f_pop3_host varchar(50) NULL,
	f_pop3_port integer NULL,
	f_smtp_host varchar(50) NULL,
	f_smtp_port integer NULL,
	f_account varchar(50) NULL,
	f_password varchar(50) NULL,
	f_ssl integer NULL,
	f_sender_name varchar(50) NULL,
	f_folder_json text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_emai__2911CBEDC6C6C8C7 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_email_receive(
	f_id varchar(50) NOT NULL,
	f_type integer NULL,
	f_maccount varchar(50) NULL,
	f_mid varchar(200) NULL,
	f_sender varchar(50) NULL,
	f_sender_name varchar(50) NULL,
	f_subject varchar(200) NULL,
	f_body_text text NULL,
	f_attachment text NULL,
	f_read integer NULL,
	f_date timestamp NULL,
	f_starred integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_emai__2911CBED20BB00AE PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_email_send(
	f_id varchar(50) NOT NULL,
	f_type integer NULL,
	f_sender text NULL,
	f_to text NULL,
	f_cc text NULL,
	f_bcc text NULL,
	f_colour varchar(50) NULL,
	f_subject varchar(200) NULL,
	f_body_text text NULL,
	f_attachment text NULL,
	f_state integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_emai__2911CBEDC5830B02 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_employee(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(50) NULL,
	f_full_name varchar(50) NULL,
	f_gender varchar(50) NULL,
	f_department_name varchar(255) NULL,
	f_position_name varchar(50) NULL,
	f_working_nature varchar(50) NULL,
	f_ID_number varchar(50) NULL,
	f_telephone varchar(50) NULL,
	f_attend_work_time timestamp NULL,
	f_birthday timestamp NULL,
	f_education varchar(50) NULL,
	f_major varchar(50) NULL,
	f_graduation_academy varchar(50) NULL,
	f_graduation_time timestamp NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_empl__2911CBED60CCCA00 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_order(
	f_id varchar(50) NOT NULL,
	f_customer_id varchar(50) NULL,
	f_customer_name varchar(50) NULL,
	f_salesman_id varchar(50) NULL,
	f_salesman_name varchar(50) NULL,
	f_order_date timestamp NULL,
	f_order_code varchar(50) NULL,
	f_transport_mode varchar(50) NULL,
	f_delivery_date timestamp NULL,
	f_delivery_address text NULL,
	f_payment_mode varchar(50) NULL,
	f_receivable_money decimal(18, 2) NULL,
	f_earnest_rate decimal(18, 2) NULL,
	f_prepay_earnest decimal(18, 2) NULL,
	f_current_state integer NULL,
	f_file_json text NULL,
	f_flow_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_inte_assistant integer NULL,
	f_dept varchar(500) NULL,
 CONSTRAINT PK__ext_orde__2911CBED1D513CD5 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_order_entry(
	f_id varchar(50) NOT NULL,
	f_order_id varchar(50) NULL,
	f_goods_id varchar(50) NULL,
	f_goods_code varchar(50) NULL,
	f_goods_name varchar(50) NULL,
	f_specifications varchar(50) NULL,
	f_unit varchar(50) NULL,
	f_qty decimal(18, 2) NULL,
	f_price decimal(18, 2) NULL,
	f_amount decimal(18, 2) NULL,
	f_discount decimal(18, 2) NULL,
	f_cess decimal(18, 2) NULL,
	f_actual_price decimal(18, 2) NULL,
	f_actual_amount decimal(18, 2) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_orde__2911CBEDD3A8E905 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_order_receivable(
	f_id varchar(50) NOT NULL,
	f_order_id varchar(50) NULL,
	f_abstract varchar(500) NULL,
	f_receivable_date timestamp NULL,
	f_receivable_rate decimal(18, 2) NULL,
	f_receivable_money decimal(18, 2) NULL,
	f_receivable_mode varchar(50) NULL,
	f_receivable_state integer NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__ext_orde__2911CBED088AB91F PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_product(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(50) NULL,
	f_type varchar(50) NULL,
	f_customer_id varchar(50) NULL,
	f_customer_name varchar(50) NULL,
	f_salesman_id varchar(50) NULL,
	f_salesman_name varchar(50) NULL,
	f_salesman_date timestamp NULL,
	f_audit_name varchar(50) NULL,
	f_audit_date timestamp NULL,
	f_audit_state integer NULL,
	f_goods_warehouse varchar(50) NULL,
	f_goods_date timestamp NULL,
	f_consignor varchar(50) NULL,
	f_goods_state integer NULL,
	f_close_state integer NULL,
	f_close_date timestamp NULL,
	f_gathering_type varchar(50) NULL,
	f_business varchar(50) NULL,
	f_address varchar(50) NULL,
	f_contact_tel varchar(50) NULL,
	f_contact_name varchar(50) NULL,
	f_harvest_msg integer NULL,
	f_harvest_warehouse varchar(50) NULL,
	f_issuing_name varchar(50) NULL,
	f_part_price decimal(18, 2) NULL,
	f_reduced_price decimal(18, 2) NULL,
	f_discount_price decimal(18, 2) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_prod__2911CBED03BA41F3 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_product_classify(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_full_name varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_prod__2911CBEDAF1B2B8A PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_product_entry(
	f_id varchar(50) NOT NULL,
	f_product_id varchar(50) NULL,
	f_product_code varchar(50) NULL,
	f_product_name varchar(50) NULL,
	f_product_specification varchar(50) NULL,
	f_qty integer NULL,
	f_command_type varchar(50) NULL,
	f_type varchar(50) NULL,
	f_money decimal(18, 2) NULL,
	f_util varchar(50) NULL,
	f_price decimal(18, 2) NULL,
	f_amount decimal(18, 2) NULL,
	f_activity varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_prod__2911CBED6BDB542A PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_product_goods(
	f_id varchar(50) NOT NULL,
	f_classify_id varchar(50) NULL,
	f_en_code varchar(50) NULL,
	f_full_name varchar(50) NULL,
	f_type varchar(50) NULL,
	f_amount decimal(18, 2) NULL,
	f_money decimal(18, 2) NULL,
	f_product_specification varchar(50) NULL,
	f_qty integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_prod__2911CBED3D239B9D PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE ext_project_gantt(
	f_id varchar(50) NOT NULL,
	f_parent_id varchar(50) NULL,
	f_project_id varchar(50) NULL,
	f_type integer NULL,
	f_en_code varchar(50) NULL,
	f_full_name varchar(50) NULL,
	f_time_limit decimal(18, 0) NULL,
	f_sign varchar(50) NULL,
	f_sign_color varchar(50) NULL,
	f_start_time timestamp NULL,
	f_end_time timestamp NULL,
	f_schedule integer NULL,
	f_manager_ids text NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_proj__2911CBED08A4E81B PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_table_example(
	f_id varchar(50) NOT NULL,
	f_interaction_date timestamp NULL,
	f_project_code varchar(50) NULL,
	f_project_name varchar(50) NULL,
	f_principal varchar(50) NULL,
	f_jack_stands varchar(50) NULL,
	f_project_type varchar(50) NULL,
	f_project_phase varchar(200) NULL,
	f_customer_name varchar(50) NULL,
	f_cost_amount decimal(18, 2) NULL,
	f_tunes_amount decimal(18, 2) NULL,
	f_projected_income decimal(18, 2) NULL,
	f_registrant varchar(50) NULL,
	f_register_date timestamp NULL,
	f_sign text NULL,
	f_postil_json text NULL,
	f_postil_count integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_tabl__2911CBEDA811DBC8 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_work_log(
	f_id varchar(50) NOT NULL,
	f_title varchar(50) NULL,
	f_today_content varchar(2000) NULL,
	f_tomorrow_content varchar(2000) NULL,
	f_question varchar(2000) NULL,
	f_to_user_id text NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_enabled_mark integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_work__2911CBED1F93991B PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE ext_work_log_share(
	f_id varchar(50) NOT NULL,
	f_work_log_id varchar(50) NULL,
	f_share_user_id varchar(50) NULL,
	f_share_time timestamp NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__ext_work__2911CBED4DE79084 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE flow_candidates(
	f_id varchar(50) NOT NULL,
	f_task_node_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_handle_id varchar(50) NULL,
	f_account varchar(50) NULL,
	f_candidates text NULL,
	f_task_operator_id varchar(50) NULL,
	f_type integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_can__2911CBED07B01EAD PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_comment(
	f_id varchar(50) NOT NULL,
	f_task_id varchar(50) NULL,
	f_text text NULL,
	f_image text NULL,
	f_file text NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_com__2911CBED046BB804 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_delegate(
	f_id varchar(50) NOT NULL,
	f_to_user_id varchar(50) NULL,
	f_to_user_name varchar(50) NULL,
	f_flow_id varchar(4000) NULL,
	f_flow_name varchar(4000) NULL,
	f_flow_category varchar(50) NULL,
	f_start_time timestamp NULL,
	f_end_time timestamp NULL,
	f_user_id varchar(50) NULL,
	f_user_name varchar(50) NULL,
	f_type integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_del__2911CBEDCFF80960 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE flow_event_log(
	f_id varchar(50) NOT NULL,
	f_task_node_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_interface_id varchar(50) NULL,
	f_result text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_eve__2911CBED840E7E4A PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_form(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(200) NULL,
	f_state integer NULL,
	f_full_name varchar(200) NULL,
	f_category varchar(50) NULL,
	f_url_address varchar(500) NULL,
	f_app_url_address varchar(500) NULL,
	f_property_json text NULL,
	f_flow_type integer NULL,
	f_form_type integer NULL,
	f_interface_url varchar(500) NULL,
	f_draft_json text NULL,
	f_db_link_id varchar(50) NULL,
	f_table_json text NULL,
	f_flow_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_for__2911CBED3737ED19 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_form_authorize(
	f_id varchar(50) NOT NULL,
	f_task_id varchar(50) NULL,
	f_node_code varchar(50) NULL,
	f_form_operate text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL
)  
;
CREATE TABLE flow_form_relation(
	f_id varchar(50) NOT NULL,
	f_flow_id varchar(50) NULL,
	f_form_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_for__2911CBED9E3A2070 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE flow_launch_user(
	f_id varchar(50) NOT NULL,
	f_organize_id text NULL,
	f_position_id text NULL,
	f_manager_id varchar(50) NULL,
	f_superior varchar(50) NULL,
	f_subordinate text NULL,
	f_task_id varchar(50) NULL,
	f_department text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_lau__2911CBED935FFF49 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_reject_data(
	f_id varchar(50) NOT NULL,
	f_task_json text NULL,
	f_task_node_json text NULL,
	f_task_operator_json text NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_rej__2911CBED06C10EDC PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_task(
	f_id varchar(50) NOT NULL,
	f_process_id varchar(50) NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_flow_urgent integer NULL,
	f_flow_id varchar(50) NULL,
	f_flow_code varchar(50) NULL,
	f_flow_name varchar(50) NULL,
	f_flow_type integer NULL,
	f_flow_version varchar(50) NULL,
	f_flow_category varchar(50) NULL,
	f_flow_form_data_json text NULL,
	f_flow_template_json text NULL,
	f_start_time timestamp NULL,
	f_end_time timestamp NULL,
	f_current_node_code varchar(2000) NULL,
	f_current_node_name varchar(2000) NULL,
	f_status integer NULL,
	f_completion integer NULL,
	f_parent_id varchar(50) NULL,
	f_is_async integer NULL,
	f_is_batch integer NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_revive_node_id varchar(50) NULL,
	f_system_id varchar(50) NULL,
	f_restore integer NULL,
	f_template_id varchar(50) NULL,
	f_delegate_user_id varchar(50) NULL,
	f_reject_data_id varchar(50) NULL,
	f_suspend integer NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBED952A6519 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_task_circulate(
	f_id varchar(50) NOT NULL,
	f_object_type varchar(50) NULL,
	f_object_id varchar(50) NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_task_node_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBED6DF63FD0 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE flow_task_node(
	f_id varchar(50) NOT NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_node_type varchar(50) NULL,
	f_node_property_json text NULL,
	f_node_up varchar(50) NULL,
	f_node_next varchar(2000) NULL,
	f_completion integer NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_state integer NULL,
	f_candidates text NULL,
	f_draft_data text NULL,
	f_form_id varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBED5DC62CA9 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_task_operator(
	f_id varchar(50) NOT NULL,
	f_append_handle_id varchar(50) NULL,
	f_handle_id varchar(50) NULL,
	f_handle_status integer NULL,
	f_handle_time timestamp NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_completion integer NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_task_node_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_type integer NULL,
	f_state integer NULL,
	f_parent_id varchar(50) NULL,
	f_draft_data text NULL,
	f_automation varchar(50) NULL,
	f_rollback_id varchar(50) NULL,
	f_reject varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBED2A2858CD PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_task_operator_record(
	f_id varchar(50) NOT NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_handle_status integer NULL,
	f_handle_id varchar(50) NULL,
	f_handle_time timestamp NULL,
	f_handle_opinion varchar(500) NULL,
	f_task_operator_id varchar(50) NULL,
	f_task_node_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_sign_img text NULL,
	f_status integer NULL,
	f_operator_id varchar(50) NULL,
	f_file_list text NULL,
	f_draft_data text NULL,
	f_approver_type varchar(50) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBED9CBBA47C PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_task_operator_user(
	f_id varchar(50) NOT NULL,
	f_append_handle_id varchar(50) NULL,
	f_handle_id varchar(50) NULL,
	f_handle_status integer NULL,
	f_handle_time timestamp NULL,
	f_node_code varchar(50) NULL,
	f_node_name varchar(50) NULL,
	f_completion integer NULL,
	f_task_node_id varchar(50) NULL,
	f_task_id varchar(50) NULL,
	f_type integer NULL,
	f_state integer NULL,
	f_parent_id varchar(50) NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_draft_data text NULL,
	f_automation varchar(50) NULL,
	f_rollback_id varchar(50) NULL,
	f_reject varchar(50) NULL,
	f_description varchar(500) NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tas__2911CBEDA901980F PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_template(
	f_id varchar(50) NOT NULL,
	f_en_code varchar(200) NULL,
	f_full_name varchar(200) NULL,
	f_type integer NULL,
	f_category varchar(50) NULL,
	f_icon varchar(50) NULL,
	f_icon_background varchar(50) NULL,
	f_description varchar(500) NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tem__2911CBED52757D7D PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE flow_template_json(
	f_id varchar(50) NOT NULL,
	f_template_id varchar(50) NULL,
	f_full_name varchar(200) NULL,
	f_visible_type integer NULL,
	f_version varchar(50) NULL,
	f_flow_template_json text NULL,
	f_group_id varchar(50) NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_send_config_ids text NULL,
	f_enabled_mark integer NULL,
	f_sort_code bigint NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_tem__2911CBED6CD4F914 PRIMARY KEY 
(
	f_id 
) 
)  
;
CREATE TABLE flow_visible(
	f_id varchar(50) NOT NULL,
	f_flow_id varchar(50) NULL,
	f_operator_type varchar(50) NULL,
	f_operator_id varchar(50) NULL,
	f_type integer NULL,
	f_sort_code bigint NULL,
	f_creator_time timestamp NULL,
	f_creator_user_id varchar(50) NULL,
	f_last_modify_time timestamp NULL,
	f_last_modify_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_mark integer NULL,
	f_tenant_id varchar(50) NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK__flow_vis__2911CBEDAE98BB16 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE mt543406707183714245(
	f_id varchar(50) NOT NULL,
	id varchar(500) NULL,
	orderNum varchar(500) NULL,
	num varchar(500) NULL,
	createTime varchar(500) NULL,
	f_inte_assistant integer NULL,
	remark varchar(50) NULL,
 CONSTRAINT PK_mt543406707183714245_f_id PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE mt543408365615710149(
	f_id varchar(50) NOT NULL,
	id varchar(500) NULL,
	orderNum varchar(500) NULL,
	num varchar(500) NULL,
	createTime varchar(500) NULL,
	f_inte_assistant integer NULL,
	mark varchar(50) NULL,
 CONSTRAINT PK_mt543408365615710149_f_id PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE mt543552698159464389(
	f_id varchar(50) NOT NULL,
	id varchar(500) NULL,
	orderNum varchar(500) NULL,
	cout varchar(500) NULL,
	createDate timestamp NULL,
	remarks varchar(500) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK_mt543552698159464389_f_id PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE mt543668771097673669(
	f_id varchar(50) NOT NULL,
	orderNum varchar(500) NULL,
	commodity varchar(500) NULL,
	count decimal(38, 15) NULL,
	price varchar(500) NULL,
	f_inte_assistant integer NULL,
	remarks varchar(50) NULL,
 CONSTRAINT PK_mt543668771097673669_f_id PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE mt543971603646513093(
	f_id varchar(50) NOT NULL,
"order" varchar(500) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK_mt543971603646513093_f_id PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE report_charts(
	ID varchar(50) NOT NULL,
	QYBM varchar(255) NULL,
	FXDMC varchar(255) NULL,
	FXDJ varchar(255) NULL,
	PGRY varchar(255) NULL,
	PGRQ varchar(255) NULL,
	FXFXFF varchar(255) NULL,
	LECE varchar(255) NULL,
	LECC varchar(255) NULL,
	LSL varchar(255) NULL,
	LSS varchar(255) NULL,
	PGFXZ varchar(255) NULL,
	FPRQ varchar(255) NULL,
	STATUS varchar(255) NULL,
	createDate varchar(255) NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__report_c__3214EC2777E00872 PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE report_department(
	id varchar(50) NOT NULL,
	departmentName varchar(255) NULL,
	departmentNum varchar(11) NULL,
	organizationName varchar(255) NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__report_d__3213E83FBAE162FA PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE report_user(
	id varchar(255) NOT NULL,
	username varchar(255) NULL,
	education varchar(255) NULL,
	sex varchar(255) NULL,
	salary decimal(10, 2) NULL,
	departmentnum varchar(255) NULL,
	peoplenum varchar(255) NULL,
	month integer NULL,
	year integer NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__report_u__3213E83FDD6479B5 PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE student(
	id varchar(50) NOT NULL,
	name varchar(50) NULL,
	age varchar(50) NULL,
	email varchar(50) NULL,
	f_inte_assistant integer NULL,
	f_flow_task_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK_student_id PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE undo_log(
	id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	branch_id bigint NOT NULL,
	xid varchar(100) NOT NULL,
	context varchar(128) NOT NULL,
	rollback_info text NOT NULL,
	log_status integer NOT NULL,
	log_created timestamp NULL,
	log_modified timestamp NULL,
	ext varchar(100) NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__undo_log__3213E83FCAF059B9 PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE wform_applybanquet(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Position varchar(50) NULL,
	F_ApplyDate timestamp NULL,
	F_BanquetNum varchar(50) NULL,
	F_BanquetPeople text NULL,
	F_Total varchar(50) NULL,
	F_Place varchar(50) NULL,
	F_ExpectedCost decimal(18, 2) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ap__2C6EC72358963AE6 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_applydelivergoods(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_CustomerName varchar(50) NULL,
	F_Contacts varchar(50) NULL,
	F_ContactPhone varchar(50) NULL,
	F_CustomerAddres varchar(50) NULL,
	F_GoodsBelonged varchar(50) NULL,
	F_InvoiceDate timestamp NULL,
	F_FreightCompany varchar(50) NULL,
	F_DeliveryType varchar(50) NULL,
	F_RransportNum varchar(50) NULL,
	F_FreightCharges decimal(18, 2) NULL,
	F_CargoInsurance decimal(18, 2) NULL,
	F_Description text NULL,
	F_InvoiceValue decimal(18, 2) NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ap__2C6EC723172A0C31 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_applydelivergoodsentry(
	F_Id varchar(50) NOT NULL,
	F_InvoiceId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit text NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_ap__2C6EC723CD3F39E0 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_applymeeting(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Position varchar(50) NULL,
	F_ConferenceName varchar(50) NULL,
	F_ConferenceTheme varchar(50) NULL,
	F_ConferenceType varchar(50) NULL,
	F_EstimatePeople varchar(50) NULL,
	F_ConferenceRoom varchar(50) NULL,
	F_Administrator varchar(50) NULL,
	F_LookPeople varchar(50) NULL,
	F_Memo varchar(50) NULL,
	F_Attendees varchar(50) NULL,
	F_ApplyMaterial varchar(50) NULL,
	F_EstimatedAmount decimal(18, 2) NULL,
	F_OtherAttendee varchar(50) NULL,
	F_StartDate timestamp NULL,
	F_EndDate timestamp NULL,
	F_FileJson text NULL,
	F_Describe text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ap__2C6EC723D240BEC8 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_archivalborrow(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_BorrowingDepartment varchar(255) NULL,
	F_ArchivesName varchar(50) NULL,
	F_ArchivalAttributes varchar(50) NULL,
	F_BorrowMode varchar(50) NULL,
	F_ApplyReason text NULL,
	F_ArchivesId varchar(50) NULL,
	F_BorrowingDate timestamp NULL,
	F_ReturnDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
	f_flow_task_id varchar(50) NULL,
	f_inte_assistant integer NULL,
 CONSTRAINT PK__wform_ar__2C6EC723164133A6 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_articleswarehous(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_Articles varchar(50) NULL,
	F_Classification varchar(50) NULL,
	F_ArticlesId varchar(50) NULL,
	F_Company varchar(50) NULL,
	F_EstimatePeople varchar(50) NULL,
	F_ApplyReasons text NULL,
	F_ApplyDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ar__2C6EC723C92E7083 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_batchpack(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ProductName varchar(50) NULL,
	F_Production varchar(50) NULL,
	F_Compactor varchar(50) NULL,
	F_CompactorDate timestamp NULL,
	F_Standard varchar(50) NULL,
	F_WarehousNo varchar(50) NULL,
	F_ProductionQuty varchar(50) NULL,
	F_OperationDate timestamp NULL,
	F_Regulations varchar(50) NULL,
	F_Packing varchar(50) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ba__2C6EC72335B719F4 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_batchtable(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FileTitle varchar(50) NULL,
	F_DraftedPerson varchar(50) NULL,
	F_FillNum varchar(50) NULL,
	F_SendUnit varchar(50) NULL,
	F_Typing varchar(50) NULL,
	F_WritingDate timestamp NULL,
	F_ShareNum varchar(50) NULL,
	F_FileJson text NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ba__2C6EC723253FE460 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_conbilling(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_Drawer varchar(50) NULL,
	F_BillDate timestamp NULL,
	F_CompanyName varchar(50) NULL,
	F_ConName varchar(50) NULL,
	F_Bank varchar(50) NULL,
	F_Amount varchar(50) NULL,
	F_BillAmount decimal(18, 2) NULL,
	F_TaxId varchar(50) NULL,
	F_InvoiceId varchar(50) NULL,
	F_InvoAddress varchar(50) NULL,
	F_PayAmount decimal(18, 2) NULL,
	F_FileJson text NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_co__2C6EC723F92FAABA PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_contractapproval(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FirstPartyUnit varchar(50) NULL,
	F_SecondPartyUnit varchar(50) NULL,
	F_FirstPartyPerson varchar(50) NULL,
	F_SecondPartyPerson varchar(50) NULL,
	F_FirstPartyContact varchar(50) NULL,
	F_SecondPartyContact varchar(50) NULL,
	F_ContractName varchar(50) NULL,
	F_ContractClass varchar(50) NULL,
	F_ContractType varchar(50) NULL,
	F_ContractId varchar(50) NULL,
	F_BusinessPerson varchar(50) NULL,
	F_IncomeAmount decimal(18, 2) NULL,
	F_InputPerson varchar(50) NULL,
	F_FileJson text NULL,
	F_PrimaryCoverage text NULL,
	F_Description text NULL,
	F_SigningDate timestamp NULL,
	F_StartDate timestamp NULL,
	F_EndDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_co__2C6EC72374C624A9 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_contractapprovalsheet(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_ApplyDate timestamp NULL,
	F_ContractId varchar(50) NULL,
	F_ContractNum varchar(50) NULL,
	F_FirstParty varchar(50) NULL,
	F_SecondParty varchar(50) NULL,
	F_ContractName varchar(50) NULL,
	F_ContractType varchar(50) NULL,
	F_PersonCharge varchar(50) NULL,
	F_LeadDepartment varchar(255) NULL,
	F_SignArea varchar(50) NULL,
	F_IncomeAmount decimal(18, 2) NULL,
	F_TotalExpenditure decimal(18, 2) NULL,
	F_ContractPeriod varchar(50) NULL,
	F_PaymentMethod varchar(50) NULL,
	F_BudgetaryApproval varchar(50) NULL,
	F_StartContractDate timestamp NULL,
	F_EndContractDate timestamp NULL,
	F_FileJson text NULL,
	F_ContractContent text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_co__2C6EC723DA2A96E1 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_debitbill(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_Departmental varchar(255) NULL,
	F_ApplyDate timestamp NULL,
	F_StaffName varchar(50) NULL,
	F_StaffPost varchar(50) NULL,
	F_StaffId varchar(50) NULL,
	F_LoanMode varchar(50) NULL,
	F_AmountDebit decimal(18, 2) NULL,
	F_TransferAccount varchar(50) NULL,
	F_RepaymentBill varchar(50) NULL,
	F_TeachingDate timestamp NULL,
	F_PaymentMethod varchar(50) NULL,
	F_Reason text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_de__2C6EC7238664A15B PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_documentapproval(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FileName varchar(50) NULL,
	F_DraftedPerson varchar(50) NULL,
	F_ServiceUnit varchar(50) NULL,
	F_FillPreparation varchar(50) NULL,
	F_FillNum varchar(50) NULL,
	F_ReceiptDate timestamp NULL,
	F_FileJson text NULL,
	F_ModifyOpinion text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_do__2C6EC723F5F3FEE2 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_documentsigning(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FileName varchar(50) NULL,
	F_FillNum varchar(50) NULL,
	F_DraftedPerson varchar(50) NULL,
	F_Reader varchar(50) NULL,
	F_FillPreparation varchar(50) NULL,
	F_CheckDate timestamp NULL,
	F_PublicationDate timestamp NULL,
	F_FileJson text NULL,
	F_DocumentContent text NULL,
	F_AdviceColumn text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_do__2C6EC723F90C58D5 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_expenseexpenditure(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_ApplyDate timestamp NULL,
	F_ContractNum varchar(50) NULL,
	F_NonContract varchar(50) NULL,
	F_AccountOpeningBank varchar(50) NULL,
	F_BankAccount varchar(50) NULL,
	F_OpenAccount varchar(50) NULL,
	F_Total decimal(18, 2) NULL,
	F_PaymentMethod varchar(50) NULL,
	F_AmountPayment decimal(18, 2) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ex__2C6EC7236C6C259D PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_finishedproduct(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_WarehouseName varchar(50) NULL,
	F_Warehouse varchar(50) NULL,
	F_Description text NULL,
	F_ReservoirDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_fi__2C6EC72368278524 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_finishedproductentry(
	F_Id varchar(50) NOT NULL,
	F_WarehouseId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit varchar(50) NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_fi__2C6EC723EC216950 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_incomerecognition(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_SettlementMonth varchar(50) NULL,
	F_CustomerName varchar(50) NULL,
	F_ContractNum varchar(50) NULL,
	F_TotalAmount decimal(18, 2) NULL,
	F_MoneyBank varchar(50) NULL,
	F_ActualAmount decimal(18, 2) NULL,
	F_ContactName varchar(50) NULL,
	F_ContacPhone varchar(50) NULL,
	F_ContactQQ varchar(50) NULL,
	F_UnpaidAmount decimal(18, 2) NULL,
	F_AmountPaid decimal(18, 2) NULL,
	F_Description text NULL,
	F_PaymentDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_in__2C6EC723AB757378 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_leaveapply(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(200) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_ApplyDept varchar(255) NULL,
	F_ApplyPost varchar(50) NULL,
	F_LeaveType varchar(50) NULL,
	F_LeaveReason text NULL,
	F_LeaveDayCount varchar(50) NULL,
	F_LeaveHour varchar(50) NULL,
	F_FileJson text NULL,
	F_Description text NULL,
	F_ApplyDate timestamp NULL,
	F_LeaveStartTime timestamp NULL,
	F_LeaveEndTime timestamp NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_le__2C6EC723B8EB5845 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_letterservice(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_HostUnit varchar(50) NULL,
	F_Title varchar(50) NULL,
	F_IssuedNum varchar(50) NULL,
	F_WritingDate timestamp NULL,
	F_ShareNum varchar(50) NULL,
	F_MainDelivery varchar(50) NULL,
	F_Copy varchar(50) NULL,
	F_FileJson text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_le__2C6EC723DFB7454A PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_materialrequisition(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_LeadPeople varchar(50) NULL,
	F_LeadDepartment varchar(255) NULL,
	F_LeadDate timestamp NULL,
	F_Warehouse varchar(50) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ma__2C6EC723E72DAAB2 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_materialrequisitionentry(
	F_Id varchar(50) NOT NULL,
	F_LeadeId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit varchar(50) NULL,
	F_MaterialDemand varchar(50) NULL,
	F_Proportioning varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_ma__2C6EC72341DBD3A3 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_monthlyreport(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_ApplyDate timestamp NULL,
	F_ApplyDept varchar(255) NULL,
	F_ApplyPost varchar(50) NULL,
	F_PlanEndTime timestamp NULL,
	F_OveralEvaluat text NULL,
	F_NPWorkMatter varchar(50) NULL,
	F_NPFinishTime timestamp NULL,
	F_NFinishMethod text NULL,
	F_FileJson text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_mo__2C6EC72305B11BD3 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_officesupplies(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(255) NULL,
	F_Department varchar(255) NULL,
	F_UseStock varchar(50) NULL,
	F_Classification varchar(50) NULL,
	F_ArticlesName varchar(50) NULL,
	F_ArticlesNum varchar(50) NULL,
	F_ArticlesId varchar(50) NULL,
	F_ApplyReasons text NULL,
	F_ApplyDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_of__2C6EC7237F92DFAF PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_outboundorder(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_CustomerName varchar(50) NULL,
	F_Warehouse varchar(50) NULL,
	F_OutStorage varchar(50) NULL,
	F_BusinessPeople varchar(50) NULL,
	F_BusinessType varchar(50) NULL,
	F_OutboundDate timestamp NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ou__2C6EC7236830D929 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_outboundorderentry(
	F_Id varchar(50) NOT NULL,
	F_OutboundId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit varchar(50) NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_ou__2C6EC723FAEC3E20 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_outgoingapply(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_ApplyDate timestamp NULL,
	F_OutgoingTotle varchar(50) NULL,
	F_StartTime timestamp NULL,
	F_EndTime timestamp NULL,
	F_Destination varchar(50) NULL,
	F_FileJson text NULL,
	F_OutgoingCause text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ou__2C6EC72381C0016C PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_paydistribution(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_Month varchar(50) NULL,
	F_IssuingUnit varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_Position varchar(50) NULL,
	F_BaseSalary decimal(18, 2) NULL,
	F_ActualAttendance varchar(50) NULL,
	F_Allowance decimal(18, 2) NULL,
	F_IncomeTax decimal(18, 2) NULL,
	F_Insurance decimal(18, 2) NULL,
	F_Performance decimal(18, 2) NULL,
	F_OvertimePay decimal(18, 2) NULL,
	F_GrossPay decimal(18, 2) NULL,
	F_Payroll decimal(18, 2) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_pa__2C6EC7232184D92E PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_paymentapply(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Departmental varchar(255) NULL,
	F_PurposeName varchar(50) NULL,
	F_ProjectCategory varchar(50) NULL,
	F_ProjectLeader varchar(50) NULL,
	F_OpeningBank varchar(50) NULL,
	F_BeneficiaryAccount varchar(50) NULL,
	F_ReceivableContact varchar(50) NULL,
	F_PaymentUnit varchar(50) NULL,
	F_ApplyAmount decimal(18, 2) NULL,
	F_SettlementMethod varchar(50) NULL,
	F_PaymentType varchar(50) NULL,
	F_AmountPaid decimal(18, 2) NULL,
	F_Description text NULL,
	F_ApplyDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_pa__2C6EC72395B86BC7 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_postbatchtab(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FileTitle varchar(50) NULL,
	F_DraftedPerson varchar(50) NULL,
	F_SendUnit varchar(50) NULL,
	F_WritingNum varchar(50) NULL,
	F_WritingDate timestamp NULL,
	F_ShareNum varchar(50) NULL,
	F_FileJson text NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_po__2C6EC7237AB35E3A PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_procurementmaterial(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Departmental varchar(255) NULL,
	F_ApplyDate timestamp NULL,
	F_PurchaseUnit varchar(50) NULL,
	F_DeliveryMode varchar(50) NULL,
	F_DeliveryAddress varchar(50) NULL,
	F_PaymentMethod varchar(50) NULL,
	F_PaymentMoney decimal(18, 2) NULL,
	F_FileJson text NULL,
	F_Reason text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_pr__2C6EC723EB6136BF PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_procurementmaterialentry(
	F_Id varchar(50) NOT NULL,
	F_ProcurementId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit text NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_pr__2C6EC7236B0A9322 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_purchaselist(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Departmental varchar(255) NULL,
	F_VendorName varchar(50) NULL,
	F_Buyer varchar(50) NULL,
	F_PurchaseDate timestamp NULL,
	F_Warehouse varchar(50) NULL,
	F_Telephone varchar(50) NULL,
	F_PaymentMethod varchar(50) NULL,
	F_PaymentMoney decimal(18, 2) NULL,
	F_FileJson text NULL,
	F_Reason text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_pu__2C6EC723359E0B80 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_purchaselistentry(
	F_Id varchar(50) NOT NULL,
	F_PurchaseId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit varchar(50) NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_pu__2C6EC72334242E9C PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_quotationapproval(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_Writer varchar(50) NULL,
	F_WriteDate timestamp NULL,
	F_CustomerName varchar(50) NULL,
	F_QuotationType varchar(50) NULL,
	F_PartnerName varchar(50) NULL,
	F_StandardFile varchar(50) NULL,
	F_CustSituation text NULL,
	F_FileJson text NULL,
	F_type varchar(255) NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_qu__2C6EC7235C08F599 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_receiptprocessing(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FileTitle varchar(50) NULL,
	F_CommunicationUnit varchar(50) NULL,
	F_LetterNum varchar(50) NULL,
	F_ReceiptDate timestamp NULL,
	F_FileJson text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_re__2C6EC7230511F594 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_receiptsign(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ReceiptTitle varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_Collector varchar(50) NULL,
	F_FileJson text NULL,
	F_ReceiptPaper text NULL,
	F_ReceiptDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_re__2C6EC723A97184B4 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_rewardpunishment(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FullName varchar(50) NULL,
	F_FillFromDate timestamp NULL,
	F_Department varchar(255) NULL,
	F_Position varchar(50) NULL,
	F_RewardPun decimal(18, 2) NULL,
	F_Reason text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_re__2C6EC723BFA7695D PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_salesorder(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_Salesman varchar(50) NULL,
	F_CustomerName varchar(50) NULL,
	F_Contacts varchar(50) NULL,
	F_ContactPhone varchar(50) NULL,
	F_CustomerAddres varchar(50) NULL,
	F_TicketNum varchar(50) NULL,
	F_InvoiceType varchar(50) NULL,
	F_PaymentMethod varchar(50) NULL,
	F_PaymentMoney decimal(18, 2) NULL,
	F_SalesDate timestamp NULL,
	F_FileJson text NULL,
	F_Description text NULL,
	F_TicketDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_sa__2C6EC723E8CA2FDA PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_salesorderentry(
	F_Id varchar(50) NOT NULL,
	F_SalesOrderId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit text NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_sa__2C6EC723922D146A PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_salessupport(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_ApplyDate timestamp NULL,
	F_ApplyDept varchar(255) NULL,
	F_Customer varchar(50) NULL,
	F_Project varchar(50) NULL,
	F_PSaleSupInfo varchar(50) NULL,
	F_StartDate timestamp NULL,
	F_EndDate timestamp NULL,
	F_PSaleSupDays varchar(50) NULL,
	F_PSalePreDays varchar(50) NULL,
	F_ConsulManager varchar(50) NULL,
	F_PSalSupConsul varchar(50) NULL,
	F_FileJson text NULL,
	F_SalSupConclu text NULL,
	F_ConsultResult text NULL,
	F_IEvaluation text NULL,
	F_Conclusion text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_sa__2C6EC7234131B677 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_staffovertime(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_ApplyDate timestamp NULL,
	F_TotleTime varchar(50) NULL,
	F_StartTime timestamp NULL,
	F_EndTime timestamp NULL,
	F_Category varchar(50) NULL,
	F_Cause text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_st__2C6EC723EAB217AC PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_supplementcard(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_FullName varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_Position varchar(50) NULL,
	F_Witness varchar(50) NULL,
	F_SupplementNum varchar(50) NULL,
	F_Description text NULL,
	F_ApplyDate timestamp NULL,
	F_StartTime timestamp NULL,
	F_EndTime timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_su__2C6EC72318DAEA21 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_travelapply(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_TravelMan varchar(50) NULL,
	F_ApplyDate timestamp NULL,
	F_Departmental varchar(255) NULL,
	F_Position varchar(50) NULL,
	F_StartDate timestamp NULL,
	F_EndDate timestamp NULL,
	F_StartPlace varchar(50) NULL,
	F_Destination varchar(50) NULL,
	F_PrepaidTravel decimal(18, 2) NULL,
	F_Description text NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_tr__2C6EC72355EF01A1 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_travelreimbursement(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_ApplyUser varchar(50) NULL,
	F_Departmental varchar(255) NULL,
	F_BillsNum varchar(50) NULL,
	F_BusinessMission text NULL,
	F_SetOutDate timestamp NULL,
	F_ReturnDate timestamp NULL,
	F_Destination varchar(50) NULL,
	F_PlaneTicket decimal(18, 2) NULL,
	F_Fare decimal(18, 2) NULL,
	F_GetAccommodation decimal(18, 2) NULL,
	F_TravelAllowance decimal(18, 2) NULL,
	F_Other decimal(18, 2) NULL,
	F_Total decimal(18, 2) NULL,
	F_ReimbursementAmount decimal(18, 2) NULL,
	F_LoanAmount decimal(18, 2) NULL,
	F_SumOfMoney decimal(18, 2) NULL,
	F_TravelerUser varchar(50) NULL,
	F_VehicleMileage decimal(18, 2) NULL,
	F_RoadFee decimal(18, 2) NULL,
	F_ParkingRate decimal(18, 2) NULL,
	F_MealAllowance decimal(18, 2) NULL,
	F_BreakdownFee decimal(18, 2) NULL,
	F_ReimbursementId varchar(50) NULL,
	F_RailTransit decimal(18, 2) NULL,
	F_ApplyDate timestamp NULL,
	f_versions integer NULL,
	f_flowtaskid varchar(50) NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_tr__2C6EC723CDEEAD85 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_vehicleapply(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_CarMan varchar(50) NULL,
	F_Department varchar(255) NULL,
	F_PlateNum varchar(50) NULL,
	F_Destination varchar(50) NULL,
	F_RoadFee decimal(18, 2) NULL,
	F_KilometreNum varchar(50) NULL,
	F_Entourage varchar(50) NULL,
	F_Description text NULL,
	F_StartDate timestamp NULL,
	F_EndDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_ve__2C6EC723855299C0 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_violationhandling(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_PlateNum varchar(50) NULL,
	F_Driver varchar(50) NULL,
	F_LeadingOfficial varchar(50) NULL,
	F_ViolationSite varchar(50) NULL,
	F_ViolationBehavior varchar(50) NULL,
	F_Deduction varchar(50) NULL,
	F_AmountMoney decimal(18, 2) NULL,
	F_Description text NULL,
	F_NoticeDate timestamp NULL,
	F_PeccancyDate timestamp NULL,
	F_LimitDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_vi__2C6EC72343CEF24F PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_warehousereceipt(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_SupplierName varchar(50) NULL,
	F_ContactPhone varchar(50) NULL,
	F_WarehousCategory varchar(50) NULL,
	F_Warehouse varchar(50) NULL,
	F_WarehousesPeople varchar(50) NULL,
	F_DeliveryNo varchar(50) NULL,
	F_WarehouseNo varchar(50) NULL,
	F_WarehousDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_wa__2C6EC723F91E7964 PRIMARY KEY 
(
	F_Id 
) 
) 
;
CREATE TABLE wform_warehousereceiptentry(
	F_Id varchar(50) NOT NULL,
	F_WarehouseId varchar(50) NULL,
	F_GoodsName varchar(50) NULL,
	F_Specifications varchar(50) NULL,
	F_Unit varchar(50) NULL,
	F_Qty varchar(50) NULL,
	F_Price decimal(18, 2) NULL,
	F_Amount decimal(18, 2) NULL,
	F_Description text NULL,
	F_SortCode bigint NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_wa__2C6EC72394157B5D PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_workcontactsheet(
	F_Id varchar(50) NOT NULL,
	F_FlowId varchar(50) NULL,
	F_FlowTitle varchar(50) NULL,
	F_FlowUrgent integer NULL,
	F_BillNo varchar(50) NULL,
	F_DrawPeople varchar(50) NULL,
	F_IssuingDepartment varchar(255) NULL,
	F_ServiceDepartment varchar(255) NULL,
	F_Recipients varchar(50) NULL,
	F_Coordination text NULL,
	F_FileJson text NULL,
	F_ToDate timestamp NULL,
	F_CollectionDate timestamp NULL,
	f_tenant_id varchar(50) NULL,
	f_flow_id varchar(50) NULL,
 CONSTRAINT PK__wform_wo__2C6EC723BA9B4938 PRIMARY KEY 
(
	F_Id 
) 
)  
;
CREATE TABLE wform_zjf_wikxqi(
	userSelectField101 varchar(255) NULL,
	f_id varchar(50) NOT NULL,
	f_tenant_id varchar(50) NULL,
 CONSTRAINT PK__wform_zj__2911CBEDB9314E36 PRIMARY KEY 
(
	f_id 
) 
) 
;
CREATE TABLE WH_BasicData(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(50) NOT NULL,
	Class integer NOT NULL,
 CONSTRAINT PK_WH_BasicData PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Bill(
	ID varchar(50) NOT NULL,
	BillCode integer NOT NULL,
	DepotID integer NOT NULL,
	StorageTypeID integer NOT NULL,
	SupplierID integer NULL,
	CustomerID integer NULL,
	DeptID integer NULL,
	Bearing integer NULL,
	CreatePersonByID integer NOT NULL,
	CreateDate timestamp NULL,
	CheckPersonByID integer NULL,
	CheckDate timestamp NULL,
	IsPrint integer NULL,
	ProjectName varchar(100) NULL,
	Flag integer NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_Bill PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_BillAutoID(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	BillAutoID integer NOT NULL,
	StorageTypeID integer NOT NULL,
 CONSTRAINT PK_WH_BillAutoID PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_BillDetail(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	SortID integer NOT NULL,
	BillID varchar(50) NOT NULL,
	MaterialID integer NOT NULL,
	MaterialCode varchar(50) NOT NULL,
	MaterialName varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 4) NOT NULL,
	TaxPrice decimal(18, 4) NOT NULL,
	Qty decimal(18, 2) NOT NULL,
	TaxRate integer NOT NULL,
	TotalPrice decimal(18, 4) NOT NULL,
	TaxTotalPrice decimal(18, 4) NOT NULL,
	Remark varchar(200) NULL,
 CONSTRAINT PK_WH_BillDetail PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_CheckBillDetail(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	SortID integer NOT NULL,
	CheckBillID varchar(50) NOT NULL,
	MaterialID integer NOT NULL,
	MaterialCode varchar(50) NOT NULL,
	MaterialName varchar(100) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 4) NOT NULL,
	Qty decimal(18, 4) NOT NULL,
	TotalPrice decimal(18, 4) NOT NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_CheckBillDetail PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Customer(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ClassID integer NOT NULL,
	Name varchar(100) NOT NULL,
	LinkMan varchar(50) NULL,
	SimpName varchar(50) NULL,
	Telephone varchar(50) NULL,
	BusinessLicence varchar(50) NULL,
	NationalTax varchar(50) NULL,
	LandTax varchar(50) NULL,
	Address varchar(50) NULL,
	Fax varchar(50) NULL,
	Zip varchar(50) NULL,
	Remark varchar(50) NULL,
 CONSTRAINT PK_WH_Client PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_CustomerClass(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(100) NOT NULL,
	ParentID integer NOT NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_ClientClass PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Depot(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(50) NOT NULL,
	ParentID char(10) NULL,
	AdministratorByID integer NULL,
	Remark varchar(50) NULL,
 CONSTRAINT PK_WH_Depot PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_DepotMaterial(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	BillID varchar(50) NOT NULL,
	MaterialID integer NOT NULL,
	StorageTypeID integer NOT NULL,
	DepotID integer NOT NULL,
	CreatePersonByID integer NOT NULL,
	CreateDate timestamp NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Qty decimal(18, 2) NOT NULL,
	Price decimal(18, 4) NOT NULL,
	TotalPrice decimal(18, 4) NOT NULL,
 CONSTRAINT PK_WH_DepotMaterial PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Dept(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(100) NOT NULL,
	DeptPersonByID integer NOT NULL,
	ParentID integer NULL,
 CONSTRAINT PK_WH_Dept PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Material(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	MaterialCode varchar(50) NOT NULL,
	MaterialName varchar(50) NOT NULL,
	BarNo varchar(50) NULL,
	ClassId integer NOT NULL,
	DepotID integer NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	UpperLimit integer NULL,
	LowerLimit integer NULL,
	InPrice decimal(18, 4) NOT NULL,
	OutPrice decimal(18, 4) NOT NULL,
	SellPrice decimal(18, 4) NOT NULL,
	Remark varchar(50) NULL,
 CONSTRAINT PK_WH_Material PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_MaterialClass(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Code varchar(50) NOT NULL,
	Name varchar(50) NOT NULL,
	FatherID integer NOT NULL,
 CONSTRAINT PK_WH_MaterialClass PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Project(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(50) NULL,
	IsEnable integer NULL,
	Remark varchar(200) NULL,
 CONSTRAINT PK_WH_Project PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_RemoveBill(
	ID varchar(50) NOT NULL,
	BillCode integer NOT NULL,
	DepotInID integer NOT NULL,
	DepotOutID integer NOT NULL,
	StorageTypeID integer NOT NULL,
	CreatePersonByID integer NOT NULL,
	CreateDate timestamp NULL,
	CheckPersonByID integer NOT NULL,
	CheckDate timestamp NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_RemoveBill PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_RemoveBillDetail(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	SortID integer NOT NULL,
	RemoveBillID varchar(50) NOT NULL,
	MaterialID integer NOT NULL,
	MaterialCode varchar(50) NULL,
	MaterialName varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 4) NOT NULL,
	Qty numeric(18, 4) NOT NULL,
	TotalPrice decimal(18, 4) NOT NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_RemoveBillDetail PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_StorageType(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(50) NOT NULL,
	Flag integer NOT NULL,
 CONSTRAINT PK_WH_StorageType PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_Supplier(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ClassID integer NULL,
	Name varchar(50) NULL,
	LinkMan varchar(50) NULL,
	Address varchar(50) NULL,
	Telephone varchar(50) NULL,
	SimpName varchar(50) NULL,
	Fax varchar(50) NULL,
	Zip varchar(50) NULL,
	Remark varchar(250) NULL,
 CONSTRAINT PK_WH_Supplier PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WH_SupplierClass(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Name varchar(50) NOT NULL,
	ParentID integer NOT NULL,
 CONSTRAINT PK_WH_Provider PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WM_BasicData(
	UnitID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UnitName varchar(50) NOT NULL,
	flag integer NOT NULL,
 CONSTRAINT PK_WM_BasicData PRIMARY KEY 
(
	UnitID 
) 
) 
;
CREATE TABLE WM_Bill(
	BillGuid varchar(50) NOT NULL,
	BillAutoID varchar(50) NOT NULL,
	BillDate timestamp NULL,
	DepotGuid varchar(200) NOT NULL,
	StorageTypeGuid varchar(200) NOT NULL,
	SupplierGuid varchar(200) NOT NULL,
	Customer varchar(50) NULL,
	DeptGuid varchar(50) NOT NULL,
	ProjectName varchar(50) NULL,
	Bearing varchar(20) NOT NULL,
	BillID varchar(50) NOT NULL,
	BatchNo varchar(50) NOT NULL,
	HandlePerson varchar(50) NOT NULL,
	CreatePerson varchar(50) NOT NULL,
	CreateDate timestamp NULL,
	CheckPerson varchar(50) NOT NULL,
	CheckDate timestamp NULL,
	Flag char(10) NOT NULL,
	Remark varchar(200) NOT NULL,
	InvoiceFlag char(10) NULL,
 CONSTRAINT PK_WM_Bill PRIMARY KEY 
(
	BillGuid 
) 
) 
;
CREATE TABLE WM_BillAutoID(
	id integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	BillAutoID integer NOT NULL,
	Flag char(10) NOT NULL,
 CONSTRAINT PK_WM_BillAutoID PRIMARY KEY 
(
	id 
) 
) 
;
CREATE TABLE WM_BillDetail(
	BillDetailGuid varchar(50) NOT NULL,
	BillGuid varchar(50) NOT NULL,
	MaterialGuid varchar(50) NOT NULL,
	MaterialId varchar(50) NOT NULL,
	MaterialName varchar(200) NOT NULL,
	BarNo varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 2) NOT NULL,
	Qty decimal(18, 2) NOT NULL,
	Total decimal(18, 2) NOT NULL,
	Remark char(10) NOT NULL,
	SortID integer NOT NULL,
	TaxRate varchar(50) NULL,
	TaxPrice decimal(18, 2) NULL,
	TaxTotal decimal(18, 2) NULL,
 CONSTRAINT PK_WM_BillDetail PRIMARY KEY 
(
	BillDetailGuid 
) 
) 
;
CREATE TABLE WM_CheckBill(
	CheckBillGuid varchar(50) NOT NULL,
	BillDate timestamp NULL,
	Depot varchar(50) NOT NULL,
	HandlePerson varchar(50) NOT NULL,
	BillID varchar(50) NOT NULL,
	BillAutoID varchar(50) NOT NULL,
	CreatePerson varchar(50) NOT NULL,
	CreateDate timestamp NULL,
	CheckPerson varchar(50) NOT NULL,
	CheckDate timestamp NULL,
	Remark varchar(200) NOT NULL,
 CONSTRAINT PK_WM_CheckBill PRIMARY KEY 
(
	CheckBillGuid 
) 
) 
;
CREATE TABLE WM_CheckBillDetail(
	CheckBillDetailGuid varchar(50) NOT NULL,
	CheckBillGuid varchar(50) NOT NULL,
	MaterialGuid varchar(50) NOT NULL,
	MaterialId varchar(50) NOT NULL,
	MaterialName varchar(200) NOT NULL,
	BarNo varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 6) NOT NULL,
	SurplusQty numeric(18, 2) NOT NULL,
	DeficientQty numeric(18, 2) NOT NULL,
	Total decimal(18, 6) NOT NULL,
	Remark char(10) NOT NULL,
	SortID integer NOT NULL,
 CONSTRAINT PK_WM_CheckBillDetail PRIMARY KEY 
(
	CheckBillDetailGuid 
) 
) 
;
CREATE TABLE WM_Client(
	Guid varchar(100) NOT NULL,
	ClientID varchar(50) NULL,
	FatherID varchar(50) NULL,
	Name varchar(100) NOT NULL,
	LinkMan varchar(50) NULL,
	SimpName char(10) NULL,
	Telephone varchar(50) NULL,
	BusinessLicence varchar(50) NULL,
	NationalTax varchar(50) NULL,
	LandTax varchar(50) NULL,
	CreateDate timestamp NULL,
	Address varchar(200) NULL,
	Fax varchar(50) NULL,
	Zip varchar(50) NULL,
	Remark varchar(100) NULL,
	Flag char(10) NULL,
 CONSTRAINT PK_WM_Client PRIMARY KEY 
(
	Guid 
) 
) 
;
CREATE TABLE WM_ClientClass(
	Num varchar(50) NOT NULL,
	Name varchar(50) NOT NULL,
	FatherNum varchar(50) NOT NULL,
	Remaek varchar(500) NULL,
 CONSTRAINT PK_WM_ClientClass PRIMARY KEY 
(
	Num 
) 
) 
;
CREATE TABLE WM_Depot(
	DepotGuid varchar(50) NOT NULL,
	DepotName varchar(200) NOT NULL,
	DepotPerson varchar(50) NOT NULL,
	Telephone varchar(100) NOT NULL,
	Remark varchar(200) NOT NULL,
 CONSTRAINT PK_WM_Depot PRIMARY KEY 
(
	DepotGuid 
) 
) 
;
CREATE TABLE WM_DepotMaterial(
	DepotMaterialGuid varchar(50) NOT NULL,
	MaterialGuID char(10) NULL,
	MaterialID varchar(50) NULL,
	MaterialName varchar(50) NULL,
	StorageTypeGuid varchar(50) NULL,
	DepotGuid varchar(50) NULL,
	CreatePerson varchar(50) NULL,
	CreateDate timestamp NULL,
	Spec varchar(50) NULL,
	Unit varchar(50) NULL,
	Qty decimal(18, 2) NULL,
	Price decimal(18, 2) NULL,
	Total decimal(18, 2) NULL,
	ClassID char(10) NULL,
	SellPrice decimal(18, 2) NULL,
 CONSTRAINT PK_WM_DepotMaterial PRIMARY KEY 
(
	DepotMaterialGuid 
) 
) 
;
CREATE TABLE WM_Dept(
	DeptGuid varchar(50) NOT NULL,
	DeptName varchar(50) NOT NULL,
	DeptPerson varchar(50) NOT NULL,
	Telephone varchar(50) NOT NULL,
	Fax varchar(50) NOT NULL,
	Address varchar(100) NOT NULL,
	Flag char(10) NULL,
 CONSTRAINT PK_WM_Dept PRIMARY KEY 
(
	DeptGuid 
) 
) 
;
CREATE TABLE WM_Employee(
	EmpGuid varchar(50) NOT NULL,
	EmpID varchar(50) NOT NULL,
	EmpName varchar(50) NOT NULL,
	Sex varchar(10) NOT NULL,
	Telephone varchar(50) NOT NULL,
	Address varchar(200) NOT NULL,
	CardID varchar(50) NOT NULL,
	Dept varchar(50) NOT NULL,
 CONSTRAINT PK_WM_Employee PRIMARY KEY 
(
	EmpGuid 
) 
) 
;
CREATE TABLE WM_Material(
	MaterialGuid varchar(50) NOT NULL,
	MaterialId varchar(50) NOT NULL,
	MaterialName varchar(100) NOT NULL,
	ClassId varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	BarNo varchar(200) NULL,
	Place varchar(50) NULL,
	Encapsulation varchar(50) NULL,
	UpperLimit integer NULL,
	LowerLimit integer NULL,
	IConsultPrice decimal(18, 2) NULL,
	EConsultPrice decimal(18, 2) NULL,
	RetailPrice decimal(18, 2) NULL,
	CalculateMethod varchar(50) NULL,
	Remark varchar(50) NULL,
	TaxRate varchar(50) NULL,
 CONSTRAINT PK_WM_Material PRIMARY KEY 
(
	MaterialGuid 
) 
) 
;
CREATE TABLE WM_Project(
	ProjectGuid char(10) NOT NULL,
	Num char(10) NULL,
	Name char(10) NULL,
	Flag char(10) NULL,
 CONSTRAINT PK_WM_Project PRIMARY KEY 
(
	ProjectGuid 
) 
) 
;
CREATE TABLE WM_RemoveBill(
	RemoveBillGuid varchar(50) NOT NULL,
	BillDate timestamp NULL,
	DepotOut varchar(50) NOT NULL,
	DepotIn varchar(50) NOT NULL,
	HandlePerson varchar(50) NOT NULL,
	BillID varchar(50) NOT NULL,
	BillAutoID varchar(50) NOT NULL,
	CreatePerson varchar(50) NOT NULL,
	CreateDate timestamp NULL,
	CheckPerson varchar(50) NOT NULL,
	CheckDate timestamp NULL,
	Remark varchar(200) NOT NULL,
 CONSTRAINT PK_WM_RemoveBill PRIMARY KEY 
(
	RemoveBillGuid 
) 
) 
;
CREATE TABLE WM_RemoveBillDetail(
	RemoveBillDetailGuid varchar(50) NOT NULL,
	RemoveBillGuid varchar(50) NOT NULL,
	MaterialGuid varchar(50) NOT NULL,
	MaterialId varchar(50) NOT NULL,
	MaterialName varchar(200) NOT NULL,
	BarNo varchar(50) NOT NULL,
	Spec varchar(50) NOT NULL,
	Unit varchar(50) NOT NULL,
	Price decimal(18, 2) NOT NULL,
	Qty numeric(18, 2) NOT NULL,
	Total decimal(18, 2) NOT NULL,
	Remark char(10) NOT NULL,
	SortID integer NOT NULL,
 CONSTRAINT PK_WM_RemoveBillDetail PRIMARY KEY 
(
	RemoveBillDetailGuid 
) 
) 
;
CREATE TABLE WM_StorageClass(
	InterID varchar(50) NOT NULL,
	InterName varchar(100) NOT NULL,
	FatherID varchar(50) NOT NULL,
	AllFatherName varchar(200) NOT NULL,
	OrderNo integer NOT NULL,
	IsVisable integer NOT NULL,
	IsDeleted integer NOT NULL,
 CONSTRAINT PK_WM_StorageClass PRIMARY KEY 
(
	InterID 
) 
) 
;
CREATE TABLE WM_StorageType(
	StorageTypeID integer NOT NULL,
	StorageTypeName varchar(50) NOT NULL,
	Flag varchar(1) NOT NULL,
 CONSTRAINT PK_WM_StorageType PRIMARY KEY 
(
	StorageTypeID 
) 
) 
;
CREATE TABLE WM_Supplier(
	Guid varchar(50) NOT NULL,
	SupplierID char(10) NULL,
	FatherID char(10) NULL,
	Name varchar(50) NULL,
	LinkMan varchar(30) NULL,
	Address varchar(50) NULL,
	Telephone varchar(50) NULL,
	SimpName varchar(50) NULL,
	Fax varchar(50) NULL,
	Zip varchar(50) NULL,
	Remark varchar(2000) NULL,
	Flag char(10) NULL,
 CONSTRAINT PK_WM_Supplier PRIMARY KEY 
(
	Guid 
) 
) 
;
CREATE TABLE WM_SupplierClass(
	ID varchar(50) NOT NULL,
	Name varchar(50) NOT NULL,
	AreaID varchar(50) NOT NULL,
	Addr varchar(50) NULL,
	Tel varchar(50) NULL,
	Email varchar(50) NULL,
	Fax varchar(50) NULL,
	LisenceNum varchar(50) NULL,
	NationalTax varchar(50) NULL,
	LandTax varchar(50) NULL,
	QQ varchar(50) NULL,
	Introduction varchar(1500) NULL,
	Ability varchar(50) NULL,
	Remark varchar(2500) NULL,
 CONSTRAINT PK_WM_Provider PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE WM_TaxRate(
	ID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	LBID integer NULL,
	TaxRate integer NULL,
	Remark varchar(50) NULL,
 CONSTRAINT PK_WM_TaxRate PRIMARY KEY 
(
	ID 
) 
) 
;
CREATE TABLE zx_sys_config(
	Id varchar(50) NOT NULL,
	Name varchar(50) NULL,
	KeyName varchar(50) NOT NULL,
	KeyValue text NULL,
	UpdateBy varchar(50) NULL,
	UpdateDate timestamp NULL,
	Comment text NULL,
	PID integer NULL,
	VersionNum integer NULL,
	f_inte_assistant integer NULL,
	f_delete_mark integer NULL,
	F_Version integer NULL,
	FormId varchar(50) NULL,
	SortCode integer NULL,
	f_delete_user_id varchar(50) NULL,
	f_delete_time timestamp NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK_zxConfig PRIMARY KEY 
(
	Id 
) 
)  
;
CREATE TABLE zx_sys_db(
	id varchar(50) NOT NULL,
	name varchar(50) NOT NULL,
	filename text NULL,
	status integer NULL,
	comment text NULL,
	f_inte_assistant integer NULL,
	f_delete_mark integer NULL,
	f_zx_system_id varchar(50) NULL,
 CONSTRAINT PK_zx_system_db PRIMARY KEY 
(
	id 
) 
)  
;
CREATE TABLE zx_system_db(
	id varchar(50) NOT NULL,
	name varchar(50) NOT NULL,
	filename text NULL,
	status integer NULL,
	comment text NULL,
	f_inte_assistant integer NULL,
	f_delete_mark integer NULL,
	f_zx_system_id varchar(50) NULL
)  
;
ALTER TABLE base_portal ALTER COLUMN f_type SET DEFAULT ('0');
ALTER TABLE base_portal ALTER COLUMN f_link_type SET DEFAULT ('0');
ALTER TABLE base_visual_release ALTER COLUMN f_enable_flow SET DEFAULT ('0');
ALTER TABLE blade_visual_category ALTER COLUMN is_deleted SET DEFAULT ('0');
ALTER TABLE WH_Bill ALTER COLUMN IsPrint SET DEFAULT ('0');
ALTER TABLE WH_BillAutoID ALTER COLUMN StorageTypeID SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WH_BillDetail ALTER COLUMN BillID SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN MaterialID SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN MaterialCode SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WH_BillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WH_BillDetail ALTER COLUMN Qty SET DEFAULT (0);
ALTER TABLE WH_BillDetail ALTER COLUMN TaxRate SET DEFAULT (0);
ALTER TABLE WH_BillDetail ALTER COLUMN TotalPrice SET DEFAULT (0);
ALTER TABLE WH_BillDetail ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WH_CheckBillDetail ALTER COLUMN CheckBillID SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN MaterialID SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN MaterialCode SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WH_CheckBillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WH_CheckBillDetail ALTER COLUMN TotalPrice SET DEFAULT (0);
ALTER TABLE WH_CheckBillDetail ALTER COLUMN Remark SET DEFAULT (0);
ALTER TABLE WH_Customer ALTER COLUMN ClassID SET DEFAULT (1);
ALTER TABLE WH_Customer ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN LinkMan SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN SimpName SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN BusinessLicence SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN NationalTax SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN LandTax SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN Fax SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN Zip SET DEFAULT NULL;
ALTER TABLE WH_Customer ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_Depot ALTER COLUMN AdministratorByID SET DEFAULT NULL;
ALTER TABLE WH_Depot ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_DepotMaterial ALTER COLUMN BillID SET DEFAULT NULL;
ALTER TABLE WH_Dept ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WH_Dept ALTER COLUMN DeptPersonByID SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN MaterialCode SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN ClassId SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WH_Material ALTER COLUMN UpperLimit SET DEFAULT (0);
ALTER TABLE WH_Material ALTER COLUMN LowerLimit SET DEFAULT (0);
ALTER TABLE WH_Material ALTER COLUMN InPrice SET DEFAULT (0);
ALTER TABLE WH_Material ALTER COLUMN OutPrice SET DEFAULT (0);
ALTER TABLE WH_Material ALTER COLUMN SellPrice SET DEFAULT (0.00);
ALTER TABLE WH_Material ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_MaterialClass ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WH_MaterialClass ALTER COLUMN FatherID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBill ALTER COLUMN BillCode SET DEFAULT NULL;
ALTER TABLE WH_RemoveBill ALTER COLUMN StorageTypeID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBill ALTER COLUMN CreatePersonByID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBill ALTER COLUMN CheckPersonByID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBill ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN RemoveBillID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN MaterialID SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN Qty SET DEFAULT (0);
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN TotalPrice SET DEFAULT (0);
ALTER TABLE WH_RemoveBillDetail ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WH_StorageType ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WH_StorageType ALTER COLUMN Flag SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN LinkMan SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN SimpName SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Fax SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Zip SET DEFAULT NULL;
ALTER TABLE WH_Supplier ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_BasicData ALTER COLUMN UnitName SET DEFAULT NULL;
ALTER TABLE WM_BasicData ALTER COLUMN flag SET DEFAULT (0);
ALTER TABLE WM_Bill ALTER COLUMN InvoiceFlag SET DEFAULT ('0');
ALTER TABLE WM_BillAutoID ALTER COLUMN Flag SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN BillGuid SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN MaterialGuid SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN MaterialId SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN BarNo SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WM_BillDetail ALTER COLUMN Qty SET DEFAULT (0);
ALTER TABLE WM_BillDetail ALTER COLUMN Total SET DEFAULT (0);
ALTER TABLE WM_BillDetail ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_BillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WM_BillDetail ALTER COLUMN TaxRate SET DEFAULT (0);
ALTER TABLE WM_CheckBill ALTER COLUMN Depot SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN HandlePerson SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN BillID SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN BillAutoID SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN CreatePerson SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN CheckPerson SET DEFAULT NULL;
ALTER TABLE WM_CheckBill ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN CheckBillGuid SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN MaterialGuid SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN MaterialId SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN BarNo SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WM_CheckBillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WM_CheckBillDetail ALTER COLUMN SurplusQty SET DEFAULT (0);
ALTER TABLE WM_CheckBillDetail ALTER COLUMN DeficientQty SET DEFAULT (0);
ALTER TABLE WM_CheckBillDetail ALTER COLUMN Total SET DEFAULT (0);
ALTER TABLE WM_CheckBillDetail ALTER COLUMN Remark SET DEFAULT (0);
ALTER TABLE WM_CheckBillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WM_Client ALTER COLUMN ClientID SET DEFAULT (1);
ALTER TABLE WM_Client ALTER COLUMN FatherID SET DEFAULT (1);
ALTER TABLE WM_Client ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN LinkMan SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN SimpName SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN BusinessLicence SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN NationalTax SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN LandTax SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN Fax SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN Zip SET DEFAULT NULL;
ALTER TABLE WM_Client ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_Depot ALTER COLUMN DepotPerson SET DEFAULT NULL;
ALTER TABLE WM_Depot ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WM_Depot ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_Dept ALTER COLUMN DeptName SET DEFAULT NULL;
ALTER TABLE WM_Dept ALTER COLUMN DeptPerson SET DEFAULT NULL;
ALTER TABLE WM_Dept ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WM_Dept ALTER COLUMN Fax SET DEFAULT NULL;
ALTER TABLE WM_Dept ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN EmpID SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN EmpName SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN Sex SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN CardID SET DEFAULT NULL;
ALTER TABLE WM_Employee ALTER COLUMN Dept SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN MaterialId SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN ClassId SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN BarNo SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN Place SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN Encapsulation SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN UpperLimit SET DEFAULT (0);
ALTER TABLE WM_Material ALTER COLUMN LowerLimit SET DEFAULT (0);
ALTER TABLE WM_Material ALTER COLUMN IConsultPrice SET DEFAULT (0);
ALTER TABLE WM_Material ALTER COLUMN EConsultPrice SET DEFAULT (0);
ALTER TABLE WM_Material ALTER COLUMN RetailPrice SET DEFAULT (0.00);
ALTER TABLE WM_Material ALTER COLUMN CalculateMethod SET DEFAULT NULL;
ALTER TABLE WM_Material ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN DepotOut SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN DepotIn SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN HandlePerson SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN BillID SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN BillAutoID SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN CreatePerson SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN CheckPerson SET DEFAULT NULL;
ALTER TABLE WM_RemoveBill ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN RemoveBillGuid SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN MaterialGuid SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN MaterialId SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN MaterialName SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN BarNo SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Spec SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Unit SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Price SET DEFAULT (0);
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Qty SET DEFAULT (0);
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Total SET DEFAULT (0);
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE WM_RemoveBillDetail ALTER COLUMN SortID SET DEFAULT (0);
ALTER TABLE WM_StorageClass ALTER COLUMN InterID SET DEFAULT NULL;
ALTER TABLE WM_StorageClass ALTER COLUMN InterName SET DEFAULT NULL;
ALTER TABLE WM_StorageClass ALTER COLUMN FatherID SET DEFAULT NULL;
ALTER TABLE WM_StorageClass ALTER COLUMN AllFatherName SET DEFAULT NULL;
ALTER TABLE WM_StorageClass ALTER COLUMN OrderNo SET DEFAULT (0);
ALTER TABLE WM_StorageClass ALTER COLUMN IsVisable SET DEFAULT (0);
ALTER TABLE WM_StorageClass ALTER COLUMN IsDeleted SET DEFAULT (0);
ALTER TABLE WM_StorageType ALTER COLUMN StorageTypeName SET DEFAULT NULL;
ALTER TABLE WM_StorageType ALTER COLUMN Flag SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN SupplierID SET DEFAULT (1);
ALTER TABLE WM_Supplier ALTER COLUMN Name SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN LinkMan SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN Address SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN Telephone SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN SimpName SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN Fax SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN Zip SET DEFAULT NULL;
ALTER TABLE WM_Supplier ALTER COLUMN Remark SET DEFAULT NULL;
ALTER TABLE zx_sys_config ALTER COLUMN VersionNum SET DEFAULT (0);
;