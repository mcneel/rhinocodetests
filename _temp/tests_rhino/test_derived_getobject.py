import Rhino


class CustomGetObject(Rhino.Input.Custom.GetObject):
    def __init__(self, filter_function):
        super().__init__()
        self.m_filter_function = filter_function

    def CustomGeometryFilter(self, rhino_object, geometry, component_index):
        rc = True
        if self.m_filter_function is not None:
            try:
                rc = self.m_filter_function(
                    rhino_object,
                    geometry,
                    component_index
                )
            except:
                rc = True
        return rc

    def Get(self):
        return super().Get()


go = CustomGetObject(None)

go.GroupSelect = False
go.AcceptNothing(True)

r = go.Get()
print(f"Objects: {r}")
