import ast
import codecs
import typing


def create_script():
    nodes = []
    nodes.append(ast.Constant("\"\"\"Test\"\"\""))
    nodes.append(ast.Import(names=[ast.alias(name='Rhino', asname=None)]))
    nodes.append(ast.Import(names=[ast.alias(name='Grasshopper', asname=None)]))
    nodes.append(ast.ImportFrom(module='Rhino.Geometry', names=[ast.alias(name='G', asname=None)], level=0))

    run_script_function = ast.FunctionDef(
        name='RunScript',
        args=ast.arguments(
            posonlyargs=[],
            args=[
                ast.arg(arg='self', annotation=None),
                ast.arg(arg='x', annotation=None),
                ast.arg(arg='y', annotation=None),
                ast.arg(arg='z', annotation=ast.Name(id='int', ctx=ast.Load()))
            ],
            kwonlyargs=[],
            kw_defaults=[],
            defaults=[],
        ),
        body=[ast.Return(value=ast.Constant(value=42))],
        decorator_list=[],
        lineno=None
    )

    class_definition = ast.ClassDef(
        name='ScriptClass',
        bases=[ast.Name(id='BaseClass')],
        keywords=[],
        body=[run_script_function],
        decorator_list=[],
        lineno=None
    )

    nodes.append(class_definition)

    return ast.Module(body=[nodes], type_ignores=[])


def create_scriptinstance(nodes: typing.Iterator[ast.AST]):
    print(nodes)


def convert_to_scriptinstance(ast_tree : ast.Module):
    for node in ast.iter_child_nodes(ast_tree):
        # print(f"{node.lineno=}")
        # print(f"{node.end_lineno=}")
        # print(f"{node.col_offset=}")
        # print(f"{node.end_col_offset=}")

        if isinstance(node, ast.Import):
            import_node: ast.Import = node
            print(f"{import_node.names=}")

        elif isinstance(node, ast.ClassDef):
            classdef_node: ast.ClassDef = node
            print(f"{classdef_node.name=}")

        elif isinstance(node, ast.FunctionDef):
            funcdef_node: ast.FunctionDef = node
            print(f"{funcdef_node.name=}")

        else:
            print(f"{node.value=}")
    

    new_script = create_script()
    
    # print(ast.dump(new_script, indent=4))
    print(ast.unparse(new_script))


with codecs.open('/Users/ein/Downloads/example_script.py', 'r', 'utf-8') as s:
    ast_tree : ast.Module = ast.parse(s.read())
    
    new_ast = convert_to_scriptinstance(ast_tree)
