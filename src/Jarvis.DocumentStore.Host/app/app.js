(function(window, angular, undefined) {'use strict';

    angular.module('admin', [
        'ui.router',
        'ui.bootstrap',
        'admin.shared',
        'admin.layout',
        'admin.dashboard',
        'admin.scheduler',
        'admin.stream',
        'admin.logs',
        'admin.recyclebin',
        'admin.handles',
        'angularUtils.directives.uiBreadcrumbs',
        'angularUtils.directives.dirPagination'
    ]);

})(window, window.angular);
