// elf-Linux-ia64-bash.h
// Generated by decompiling elf-Linux-ia64-bash
// using Reko decompiler version 0.11.4.0.

/*
// Equivalence classes ////////////
Eq_1: (struct "Globals"
		(A1C0 word64 secondary_prompt)
		(A1C8 word64 primary_prompt)
		(A1E0 word64 bash_getcwd_errstr)
		(A228 word64 the_current_maintainer)
		(A274 word64 pgrp_pipe)
		(A320 word64 bash_license)
		(A328 word64 bash_copyright)
		(A330 word64 sccs_version)
		(A338 word64 release_status)
		(A348 word64 dist_version)
		(A350 word64 bash_badsub_errmsg)
		(A390 word64 shell_builtins)
		(A4C8 word64 shell_name)
		(A4F8 word64 current_host_name)
		(A500 word64 global_command)
		(A560 word64 pushed_string_list)
		(A568 word64 stream_list)
		(A580 word64 current_readline_line)
		(A588 word64 current_readline_prompt)
		(A598 word64 prompt_string_pointer)
		(A690 word64 xtrace_fp)
		(A6A0 word64 the_printed_command)
		(A6E0 word64 current_fds_to_close)
		(A710 word64 exec_redirection_undo_list)
		(A718 word64 redirection_undo_list)
		(A748 word64 export_env)
		(A758 word64 rest_of_args)
		(A760 word64 temporary_env)
		(A770 word64 shell_function_defs)
		(A778 word64 shell_functions)
		(A780 word64 shell_variables)
		(A788 word64 global_variables)
		(A880 word64 the_pipeline)
		(A890 word64 jobs)
		(A8F0 word64 subst_assign_varlist)
		(A928 word64 hashed_filenames)
		(A9B8 word64 aliases)
		(AC00 word64 prog_completes)
		(AC38 word64 the_current_working_directory)
		(AC40 word64 this_shell_builtin)
		(AC48 word64 last_shell_builtin)
		(AD58 word64 sh_optarg)
		(AD70 word64 lcurrent)
		(AE08 word64 __libc_ia64_register_backing_store_base)
		(AE10 word64 subshell_envp)
		(AE18 word64 subshell_argv)
		(AE28 word64 exec_argv0)
		(AE38 word64 shell_start_time)
		(AE48 word64 command_execution_string)
		(AE58 word64 shell_environment)
		(AE80 word64 ps2_prompt)
		(AE88 word64 ps1_prompt)
		(AE98 word64 current_prompt_string)
		(AEA8 word64 the_printed_command_except_trap)
		(AEB8 word64 this_shell_function)
		(AEC8 word64 this_command_name)
		(AED8 word64 tempvar_list)
		(AEF0 word64 ifs_var)
		(AF00 word64 ifs_value)
		(AF10 word64 ifs_firstc_len)
		(AF68 word64 global_error_list)
		(AF78 word64 pcomp_curcmd)
		(AF80 word64 pcomp_curcs)
		(AF88 word64 current_builtin)
		(AFA0 word64 list_optarg)
		(AFB0 word64 loptend)
		(AFC0 word64 glob_error_return))
	globals_t (in globals : (ptr64 (struct "Globals")))
// Type Variables ////////////
globals_t: (in globals : (ptr64 (struct "Globals")))
  Class: Eq_1
  DataType: (ptr64 Eq_1)
  OrigDataType: (ptr64 (struct "Globals"))
T_2:
  Class: Eq_2
  DataType: word64
  OrigDataType: word64
T_3:
  Class: Eq_3
  DataType: word64
  OrigDataType: word64
T_4:
  Class: Eq_4
  DataType: word64
  OrigDataType: word64
T_5:
  Class: Eq_5
  DataType: word64
  OrigDataType: word64
T_6:
  Class: Eq_6
  DataType: word64
  OrigDataType: word64
T_7:
  Class: Eq_7
  DataType: word64
  OrigDataType: word64
T_8:
  Class: Eq_8
  DataType: word64
  OrigDataType: word64
T_9:
  Class: Eq_9
  DataType: word64
  OrigDataType: word64
T_10:
  Class: Eq_10
  DataType: word64
  OrigDataType: word64
T_11:
  Class: Eq_11
  DataType: word64
  OrigDataType: word64
T_12:
  Class: Eq_12
  DataType: word64
  OrigDataType: word64
T_13:
  Class: Eq_13
  DataType: word64
  OrigDataType: word64
T_14:
  Class: Eq_14
  DataType: word64
  OrigDataType: word64
T_15:
  Class: Eq_15
  DataType: word64
  OrigDataType: word64
T_16:
  Class: Eq_16
  DataType: word64
  OrigDataType: word64
T_17:
  Class: Eq_17
  DataType: word64
  OrigDataType: word64
T_18:
  Class: Eq_18
  DataType: word64
  OrigDataType: word64
T_19:
  Class: Eq_19
  DataType: word64
  OrigDataType: word64
T_20:
  Class: Eq_20
  DataType: word64
  OrigDataType: word64
T_21:
  Class: Eq_21
  DataType: word64
  OrigDataType: word64
T_22:
  Class: Eq_22
  DataType: word64
  OrigDataType: word64
T_23:
  Class: Eq_23
  DataType: word64
  OrigDataType: word64
T_24:
  Class: Eq_24
  DataType: word64
  OrigDataType: word64
T_25:
  Class: Eq_25
  DataType: word64
  OrigDataType: word64
T_26:
  Class: Eq_26
  DataType: word64
  OrigDataType: word64
T_27:
  Class: Eq_27
  DataType: word64
  OrigDataType: word64
T_28:
  Class: Eq_28
  DataType: word64
  OrigDataType: word64
T_29:
  Class: Eq_29
  DataType: word64
  OrigDataType: word64
T_30:
  Class: Eq_30
  DataType: word64
  OrigDataType: word64
T_31:
  Class: Eq_31
  DataType: word64
  OrigDataType: word64
T_32:
  Class: Eq_32
  DataType: word64
  OrigDataType: word64
T_33:
  Class: Eq_33
  DataType: word64
  OrigDataType: word64
T_34:
  Class: Eq_34
  DataType: word64
  OrigDataType: word64
T_35:
  Class: Eq_35
  DataType: word64
  OrigDataType: word64
T_36:
  Class: Eq_36
  DataType: word64
  OrigDataType: word64
T_37:
  Class: Eq_37
  DataType: word64
  OrigDataType: word64
T_38:
  Class: Eq_38
  DataType: word64
  OrigDataType: word64
T_39:
  Class: Eq_39
  DataType: word64
  OrigDataType: word64
T_40:
  Class: Eq_40
  DataType: word64
  OrigDataType: word64
T_41:
  Class: Eq_41
  DataType: word64
  OrigDataType: word64
T_42:
  Class: Eq_42
  DataType: word64
  OrigDataType: word64
T_43:
  Class: Eq_43
  DataType: word64
  OrigDataType: word64
T_44:
  Class: Eq_44
  DataType: word64
  OrigDataType: word64
T_45:
  Class: Eq_45
  DataType: word64
  OrigDataType: word64
T_46:
  Class: Eq_46
  DataType: word64
  OrigDataType: word64
T_47:
  Class: Eq_47
  DataType: word64
  OrigDataType: word64
T_48:
  Class: Eq_48
  DataType: word64
  OrigDataType: word64
T_49:
  Class: Eq_49
  DataType: word64
  OrigDataType: word64
T_50:
  Class: Eq_50
  DataType: word64
  OrigDataType: word64
T_51:
  Class: Eq_51
  DataType: word64
  OrigDataType: word64
T_52:
  Class: Eq_52
  DataType: word64
  OrigDataType: word64
T_53:
  Class: Eq_53
  DataType: word64
  OrigDataType: word64
T_54:
  Class: Eq_54
  DataType: word64
  OrigDataType: word64
T_55:
  Class: Eq_55
  DataType: word64
  OrigDataType: word64
T_56:
  Class: Eq_56
  DataType: word64
  OrigDataType: word64
T_57:
  Class: Eq_57
  DataType: word64
  OrigDataType: word64
T_58:
  Class: Eq_58
  DataType: word64
  OrigDataType: word64
T_59:
  Class: Eq_59
  DataType: word64
  OrigDataType: word64
T_60:
  Class: Eq_60
  DataType: word64
  OrigDataType: word64
T_61:
  Class: Eq_61
  DataType: word64
  OrigDataType: word64
T_62:
  Class: Eq_62
  DataType: word64
  OrigDataType: word64
T_63:
  Class: Eq_63
  DataType: word64
  OrigDataType: word64
T_64:
  Class: Eq_64
  DataType: word64
  OrigDataType: word64
T_65:
  Class: Eq_65
  DataType: word64
  OrigDataType: word64
T_66:
  Class: Eq_66
  DataType: word64
  OrigDataType: word64
T_67:
  Class: Eq_67
  DataType: word64
  OrigDataType: word64
T_68:
  Class: Eq_68
  DataType: word64
  OrigDataType: word64
*/
typedef struct Globals {
	word64 secondary_prompt;	// A1C0
	word64 primary_prompt;	// A1C8
	word64 bash_getcwd_errstr;	// A1E0
	word64 the_current_maintainer;	// A228
	word64 pgrp_pipe;	// A274
	word64 bash_license;	// A320
	word64 bash_copyright;	// A328
	word64 sccs_version;	// A330
	word64 release_status;	// A338
	word64 dist_version;	// A348
	word64 bash_badsub_errmsg;	// A350
	word64 shell_builtins;	// A390
	word64 shell_name;	// A4C8
	word64 current_host_name;	// A4F8
	word64 global_command;	// A500
	word64 pushed_string_list;	// A560
	word64 stream_list;	// A568
	word64 current_readline_line;	// A580
	word64 current_readline_prompt;	// A588
	word64 prompt_string_pointer;	// A598
	word64 xtrace_fp;	// A690
	word64 the_printed_command;	// A6A0
	word64 current_fds_to_close;	// A6E0
	word64 exec_redirection_undo_list;	// A710
	word64 redirection_undo_list;	// A718
	word64 export_env;	// A748
	word64 rest_of_args;	// A758
	word64 temporary_env;	// A760
	word64 shell_function_defs;	// A770
	word64 shell_functions;	// A778
	word64 shell_variables;	// A780
	word64 global_variables;	// A788
	word64 the_pipeline;	// A880
	word64 jobs;	// A890
	word64 subst_assign_varlist;	// A8F0
	word64 hashed_filenames;	// A928
	word64 aliases;	// A9B8
	word64 prog_completes;	// AC00
	word64 the_current_working_directory;	// AC38
	word64 this_shell_builtin;	// AC40
	word64 last_shell_builtin;	// AC48
	word64 sh_optarg;	// AD58
	word64 lcurrent;	// AD70
	word64 __libc_ia64_register_backing_store_base;	// AE08
	word64 subshell_envp;	// AE10
	word64 subshell_argv;	// AE18
	word64 exec_argv0;	// AE28
	word64 shell_start_time;	// AE38
	word64 command_execution_string;	// AE48
	word64 shell_environment;	// AE58
	word64 ps2_prompt;	// AE80
	word64 ps1_prompt;	// AE88
	word64 current_prompt_string;	// AE98
	word64 the_printed_command_except_trap;	// AEA8
	word64 this_shell_function;	// AEB8
	word64 this_command_name;	// AEC8
	word64 tempvar_list;	// AED8
	word64 ifs_var;	// AEF0
	word64 ifs_value;	// AF00
	word64 ifs_firstc_len;	// AF10
	word64 global_error_list;	// AF68
	word64 pcomp_curcmd;	// AF78
	word64 pcomp_curcs;	// AF80
	word64 current_builtin;	// AF88
	word64 list_optarg;	// AFA0
	word64 loptend;	// AFB0
	word64 glob_error_return;	// AFC0
} Eq_1;

